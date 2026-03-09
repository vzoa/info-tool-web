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
    public string Name => "route";
    public string[] Aliases => ["rt"];
    public string Summary => "Look up routes between airports";
    public string Usage => "route <departure> <arrival>\n" +
                           "    route KSFO KLAX  — Preferred + real-world routes";

    public async Task<CommandResult> ExecuteAsync(CommandArgs args)
    {
        if (args.Positional.Length < 2)
        {
            return CommandResult.FromError("Usage: route <departure> <arrival>");
        }

        var dep = AirportIdHelper.NormalizeToIcao(args.Positional[0]);
        var arr = AirportIdHelper.NormalizeToIcao(args.Positional[1]);

        var sb = new StringBuilder();

        // Preferred routes (alias rules)
        var aliasRules = aliasRouteRuleRepository.GetAllRules()
            .Where(r => MatchesAirport(r.DepartureAirport, dep) && MatchesAirport(r.ArrivalAirport, arr))
            .ToList();

        if (aliasRules.Count > 0)
        {
            var widths = new[] { 12, 68 };
            sb.Append(TextFormatter.FormatTableHeader($"Preferred Routes: {dep} → {arr}", ["Type", "Route"], widths));
            foreach (var rule in aliasRules)
            {
                var type = FormatAircraftTypes(rule.AllowedAircraftTypes);
                sb.AppendLine(TextFormatter.FormatTableRow([type, rule.Route], widths));
            }
            sb.AppendLine();
        }

        // LOA routes
        var loaRules = loaRuleRepository.GetAllRules()
            .Where(r => r.DepartureAirportRegex.IsMatch(dep) && r.ArrivalAirportRegex.IsMatch(arr))
            .ToList();

        if (loaRules.Count > 0)
        {
            var widths = new[] { 8, 60, 12 };
            sb.Append(TextFormatter.FormatTableHeader($"LOA Routes: {dep} → {arr}", ["RNAV", "Route", "Notes"], widths));
            foreach (var rule in loaRules)
            {
                var rnav = rule.IsRnavRequired ? "Yes" : "No";
                sb.AppendLine(TextFormatter.FormatTableRow([rnav, rule.Route, rule.Notes ?? ""], widths));
            }
            sb.AppendLine();
        }

        // Real-world routes from FlightAware
        try
        {
            var routeData = await flightAwareRouteService.FetchRoutesAsync(dep, arr);
            if (routeData.FlightRouteSummaries.Count > 0)
            {
                var widths = new[] { 8, 10, 62 };
                sb.Append(TextFormatter.FormatTableHeader($"Real-World Routes: {dep} → {arr}", ["Freq", "Altitude", "Route"], widths));
                foreach (var route in routeData.FlightRouteSummaries.OrderByDescending(r => r.RouteFrequency).Take(10))
                {
                    var alt = FormatAltRange(route.MinAltitude, route.MaxAltitude);
                    sb.AppendLine(TextFormatter.FormatTableRow(
                        [route.RouteFrequency.ToString(), alt, Truncate(route.Route, 60)],
                        widths));
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
