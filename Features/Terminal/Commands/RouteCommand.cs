using System.Text;
using ZoaReference.Features.Routes.Models;
using ZoaReference.Features.Routes.Repositories;
using ZoaReference.Features.Routes.Services;
using ZoaReference.Features.Terminal.Services;

namespace ZoaReference.Features.Terminal.Commands;

public class RouteCommand(
    FlightAwareRouteService flightAwareRouteService,
    AliasRouteRuleRepository aliasRouteRuleRepository,
    LoaRuleRepository loaRuleRepository) : ITerminalCommand
{
    private const int DefaultMaxRoutes = 5;

    public string Name => "route";
    public string[] Aliases => ["rt"];
    public string Summary => "Look up routes between airports";
    public string Usage => "route <departure> <arrival>\n" +
                           "    route SFO LAX         — Preferred + top 5 real-world routes\n" +
                           "    route SFO LAX -a      — Show all real-world routes\n" +
                           "    route SFO LAX -n 10   — Show top 10 real-world routes";

    public async Task<CommandResult> ExecuteAsync(CommandArgs args)
    {
        if (args.Positional.Length < 2)
        {
            return CommandResult.FromError("Usage: route <departure> <arrival>");
        }

        var dep = AirportIdHelper.NormalizeToIcao(args.Positional[0]);
        var arr = AirportIdHelper.NormalizeToIcao(args.Positional[1]);
        var showAll = args.Flags.ContainsKey("all") || args.Flags.ContainsKey("a");
        var maxRoutes = showAll ? (int?)null : DefaultMaxRoutes;
        if (args.Flags.TryGetValue("n", out var nVal) && int.TryParse(nVal, out var n))
        {
            maxRoutes = n;
        }

        var sb = new StringBuilder();

        // Preferred routes (alias rules)
        var aliasRules = aliasRouteRuleRepository.GetAllRules()
            .Where(r => MatchesAirport(r.DepartureAirport, dep) && MatchesAirport(r.ArrivalAirport, arr))
            .ToList();

        if (aliasRules.Count > 0)
        {
            var widths = new[] { 10, 10, 12, 48 };
            sb.Append(TextFormatter.FormatTableHeader(
                $"Preferred Routes: {dep} → {arr}",
                ["Dep Rwy", "Arr Rwy", "Types", "Route"], widths));
            foreach (var rule in aliasRules)
            {
                var type = FormatAircraftTypes(rule.AllowedAircraftTypes);
                var depRwy = rule.DepartureRunway?.ToString() ?? "";
                var arrRwy = rule.ArrivalRunway?.ToString() ?? "";
                sb.AppendLine(TextFormatter.FormatTableRow([depRwy, arrRwy, type, rule.Route], widths));
            }
            sb.AppendLine();
        }

        // LOA routes
        var loaRules = loaRuleRepository.GetAllRules()
            .Where(r => r.DepartureAirportRegex.IsMatch(dep) && r.ArrivalAirportRegex.IsMatch(arr))
            .ToList();

        if (loaRules.Count > 0)
        {
            var widths = new[] { 36, 8, 36 };
            sb.Append(TextFormatter.FormatTableHeader(
                $"LOA Routes: {dep} → {arr}",
                ["Route", "RNAV?", "Notes"], widths));
            foreach (var rule in loaRules)
            {
                var rnav = rule.IsRnavRequired ? "Yes" : "No";
                sb.AppendLine(TextFormatter.FormatTableRow(
                    [Truncate(rule.Route, 34), rnav, rule.Notes ?? ""], widths));
            }
            sb.AppendLine();
        }

        // Real-world routes from FlightAware
        try
        {
            var routeData = await flightAwareRouteService.FetchRoutesAsync(dep, arr);
            var allRoutes = routeData.FlightRouteSummaries
                .OrderByDescending(r => r.RouteFrequency)
                .ToList();

            if (allRoutes.Count > 0)
            {
                var displayRoutes = maxRoutes.HasValue ? allRoutes.Take(maxRoutes.Value).ToList() : allRoutes;
                var widths = new[] { 10, 46, 10 };
                sb.Append(TextFormatter.FormatTableHeader(
                    $"Real-World Routes: {dep} → {arr}",
                    ["Freq", "Route", "Altitude"], widths));
                foreach (var route in displayRoutes)
                {
                    var alt = FormatAltRange(route.MinAltitude, route.MaxAltitude);
                    sb.AppendLine(TextFormatter.FormatTableRow(
                        [route.RouteFrequency.ToString(), Truncate(route.Route, 44), alt],
                        widths));
                }

                if (maxRoutes.HasValue && allRoutes.Count > maxRoutes.Value)
                {
                    sb.AppendLine(TextFormatter.Colorize(
                        $"  Showing top {maxRoutes.Value} of {allRoutes.Count} routes (use -a for all)",
                        AnsiColor.Gray));
                }
            }
        }
        catch (HttpRequestException)
        {
            sb.AppendLine(TextFormatter.Colorize("  Could not fetch real-world routes from FlightAware.", AnsiColor.Yellow));
        }

        if (sb.Length == 0)
        {
            return CommandResult.FromError($"No routes found for {dep} → {arr}");
        }

        return CommandResult.FromText(sb.ToString());
    }

    private static bool MatchesAirport(string ruleAirport, string icao)
    {
        return ruleAirport.Equals(icao, StringComparison.OrdinalIgnoreCase) ||
               ruleAirport.Equals(AirportIdHelper.NormalizeToFaa(icao), StringComparison.OrdinalIgnoreCase);
    }

    private static string FormatAircraftTypes(AliasRouteRule.RouteAircraftTypes types)
    {
        var parts = new List<string>();
        if (types.HasFlag(AliasRouteRule.RouteAircraftTypes.Jet)) parts.Add("J");
        if (types.HasFlag(AliasRouteRule.RouteAircraftTypes.Turboprop)) parts.Add("T");
        if (types.HasFlag(AliasRouteRule.RouteAircraftTypes.Prop)) parts.Add("P");
        return string.Join("/", parts);
    }

    private static string FormatAltRange(int? min, int? max)
    {
        if (min is null && max is null) return "-";
        if (min == max) return $"FL{min / 100}";
        return $"FL{min / 100}-{max / 100}";
    }

    private static string Truncate(string text, int maxLen) =>
        text.Length <= maxLen ? text : text[..(maxLen - 3)] + "...";

    public IEnumerable<string> GetCompletions(string partial, int argIndex) => [];
}
