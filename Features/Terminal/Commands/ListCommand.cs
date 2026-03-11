using System.Text;
using ZoaReference.Features.Charts.Services;
using ZoaReference.Features.IcaoReference.Repositories;
using ZoaReference.Features.Terminal.Services;

namespace ZoaReference.Features.Terminal.Commands;

public class ListCommand(AviationApiChartService chartService, AirportRepository airportRepository) : ITerminalCommand
{
    private static readonly Dictionary<string, string> ChartTypeAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SID"] = "DP",
        ["APP"] = "IAP",
        ["TAXI"] = "APD",
    };

    private static readonly HashSet<string> ValidChartTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "DP", "STAR", "IAP", "APD"
    };

    public string Name => "list";
    public string[] Aliases => ["ls"];
    public string Summary => "List charts for an airport";
    public string Usage => "list <airport> [type] [search]\n" +
                           "    list OAK           — All charts for OAK\n" +
                           "    list SFO DP        — SFO departure procedures\n" +
                           "    list SFO SID       — Same (SID is alias for DP)\n" +
                           "    list OAK STAR      — OAK arrival procedures\n" +
                           "    list SJC IAP       — SJC instrument approaches\n" +
                           "    list SJC APP       — Same (APP is alias for IAP)\n" +
                           "    list RNO APD       — RNO airport diagrams\n" +
                           "    list RNO TAXI      — Same (TAXI is alias for APD)\n" +
                           "    list SMF APP TENCO — Search SMF approaches for TENCO";

    public async Task<CommandResult> ExecuteAsync(CommandArgs args)
    {
        if (args.Positional.Length < 1)
        {
            return CommandResult.FromError("Usage: list <airport> [type] [search]");
        }

        var airportId = AirportIdHelper.NormalizeToIcao(args.Positional[0]);
        var charts = await chartService.GetChartsForId(airportId);

        if (charts.Count == 0)
        {
            return CommandResult.FromError($"No charts found for {airportId}");
        }

        // Filter out continuation pages
        var chartList = charts
            .Where(c => !c.ChartName.Contains(", CONT."))
            .ToList();

        // Parse optional chart type and search term
        string? filterType = null;
        string? searchTerm = null;

        if (args.Positional.Length >= 2)
        {
            var typeArg = args.Positional[1].ToUpperInvariant();

            // Check if it's a chart type or alias
            if (ValidChartTypes.Contains(typeArg))
            {
                filterType = typeArg;
            }
            else if (ChartTypeAliases.TryGetValue(typeArg, out var mapped))
            {
                filterType = mapped;
            }
            else
            {
                // Not a type — treat everything after airport as search
                searchTerm = string.Join(" ", args.Positional[1..]);
            }

            // If type was found, remaining args are search term
            if (filterType is not null && args.Positional.Length >= 3)
            {
                searchTerm = string.Join(" ", args.Positional[2..]);
            }
        }

        // Filter by chart type
        if (filterType is not null)
        {
            chartList = chartList
                .Where(c => c.ChartCode.Equals(filterType, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        // Filter by search term (substring match on chart name)
        if (searchTerm is not null)
        {
            chartList = chartList
                .Where(c => c.ChartName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (chartList.Count == 0)
        {
            var context = filterType is not null && searchTerm is not null
                ? $"No {filterType} charts containing '{searchTerm}' found for {airportId}"
                : filterType is not null
                    ? $"No {filterType} charts found for {airportId}"
                    : searchTerm is not null
                        ? $"No charts containing '{searchTerm}' found for {airportId}"
                        : $"No charts found for {airportId}";
            return CommandResult.FromError(context);
        }

        // Format output
        var sb = new StringBuilder();
        var displayType = filterType ?? "all";
        var title = searchTerm is not null
            ? $"Charts containing '{searchTerm}' — {airportId}"
            : $"{displayType.ToUpperInvariant()} charts — {airportId}";

        if (filterType is not null)
        {
            // Single type — just list names
            var widths = new[] { 60 };
            sb.Append(TextFormatter.FormatTableHeader(title, ["Chart Name"], widths));
            foreach (var chart in chartList.OrderBy(c => c.ChartName))
            {
                sb.AppendLine(TextFormatter.FormatTableRow([chart.ChartName], widths));
            }
        }
        else
        {
            // All types — group by chart code
            var widths = new[] { 8, 52 };
            sb.Append(TextFormatter.FormatTableHeader(title, ["Type", "Chart Name"], widths));
            foreach (var chart in chartList.OrderBy(c => ChartCodeOrder(c.ChartCode)).ThenBy(c => c.ChartName))
            {
                sb.AppendLine(TextFormatter.FormatTableRow([chart.ChartCode, chart.ChartName], widths));
            }
        }

        sb.AppendLine();
        sb.AppendLine($"  {TextFormatter.Colorize($"{chartList.Count} charts", AnsiColor.Gray)}");

        return CommandResult.FromText(sb.ToString());
    }

    private static int ChartCodeOrder(string code) => code.ToUpperInvariant() switch
    {
        "APD" => 0,
        "DP" => 1,
        "STAR" => 2,
        "IAP" => 3,
        _ => 4
    };

    public IEnumerable<string> GetCompletions(string partial, int argIndex)
    {
        if (argIndex <= 1)
        {
            return airportRepository.AllAirportIds
                .Where(id => id.StartsWith(partial, StringComparison.OrdinalIgnoreCase));
        }
        if (argIndex == 2)
        {
            var types = new[] { "DP", "SID", "STAR", "IAP", "APP", "APD", "TAXI" };
            return types.Where(t => t.StartsWith(partial, StringComparison.OrdinalIgnoreCase));
        }
        return [];
    }
}
