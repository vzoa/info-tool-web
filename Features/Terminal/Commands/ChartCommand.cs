using System.Text;
using System.Text.RegularExpressions;
using ZoaReference.Features.Charts.Services;
using ZoaReference.Features.IcaoReference.Repositories;
using ZoaReference.Features.Nasr.Services;
using ZoaReference.Features.Terminal.Services;

namespace ZoaReference.Features.Terminal.Commands;

public partial class ChartCommand(
    AviationApiChartService chartService,
    AirportRepository airportRepository,
    ChartPdfProcessingService chartPdfProcessingService,
    NasrDataService nasrDataService) : ITerminalCommand
{
    private static readonly Dictionary<string, string> DigitToWord = new()
    {
        ["1"] = "ONE", ["2"] = "TWO", ["3"] = "THREE", ["4"] = "FOUR",
        ["5"] = "FIVE", ["6"] = "SIX", ["7"] = "SEVEN", ["8"] = "EIGHT", ["9"] = "NINE",
    };

    // Aliases applied to the whole filter string (e.g. "TAXI" → "AIRPORT DIAGRAM")
    // so users can type shorthand chart names. Mirrors Python charts.py:_normalize_chart_name.
    private static readonly Dictionary<string, string> ChartNameAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["TAXI"] = "AIRPORT DIAGRAM",
        ["DVA"] = "DIVERSE VECTOR AREA",
    };

    // Query tokens that indicate a specific chart type; charts matching the
    // detected type get a +0.15 score bonus to help break fuzzy-match ties.
    // Approach-type list mirrors CifpService.ApproachTypeCodes (ILS, LOC, VOR,
    // RNAV, RNP, GPS, NDB, LDA, SDF, TACAN) plus GLS for GBAS landings.
    private static readonly HashSet<string> IapTriggers =
        new(StringComparer.Ordinal)
        {
            "ILS", "LOC", "VOR", "RNAV", "RNP", "GPS", "NDB",
            "GLS", "LDA", "SDF", "TACAN",
            "APPROACH", "APP",
        };
    private static readonly HashSet<string> DpTriggers =
        new(StringComparer.Ordinal) { "DEPARTURE", "DEP", "SID" };
    private static readonly HashSet<string> StarTriggers =
        new(StringComparer.Ordinal) { "ARRIVAL", "ARR", "STAR" };
    private static readonly HashSet<string> ApdTriggers =
        new(StringComparer.Ordinal) { "TAXI", "APD", "DIAGRAM" };

    public string Name => "chart";
    public string[] Aliases => ["charts"];
    public string Summary => "Look up airport charts";
    public string Usage => "chart <airport> [type] [search]\n" +
                           "    chart SFO          — All charts for SFO\n" +
                           "    chart SFO dp       — Filter by chart code (dp, star, iap, apd)\n" +
                           "    chart SFO ILS 28   — Search chart names\n" +
                           "    chart SFO dyamd5   — Fuzzy search (matches DYAMD FIVE)";

    public async Task<CommandResult> ExecuteAsync(CommandArgs args)
    {
        if (args.Positional.Length < 1)
        {
            return CommandResult.FromError("Usage: chart <airport> [type] [search]");
        }

        var airportId = AirportIdHelper.NormalizeToIcao(args.Positional[0]);
        var charts = await chartService.GetChartsForId(airportId);

        if (charts.Count == 0)
        {
            return CommandResult.FromError($"No charts found for {airportId}");
        }

        var chartList = charts.ToList();

        // No filter — list everything for the airport.
        if (args.Positional.Length < 2)
        {
            return chartList.Count == 1
                ? await OpenSingleChart(chartList[0])
                : await FormatChartList(airportId, chartList);
        }

        var rawFilter = string.Join(" ", args.Positional[1..]);
        // Normalize the filter so shorthand queries (TAXI, FMG1, DYAMD5, RNAV 4L)
        // match real chart names like "AIRPORT DIAGRAM", "MUSTANG ONE",
        // "DYAMD FIVE", "RNAV (GPS) RWY 04L".
        var filter = await NormalizeChartQuery(rawFilter);

        // `chart SFO dp` → list all DP charts, no fuzzy match needed.
        if (args.Positional.Length == 2)
        {
            var codeFilter = args.Positional[1].ToUpperInvariant();
            var codeFiltered = chartList
                .Where(c => c.ChartCode.Equals(codeFilter, StringComparison.OrdinalIgnoreCase))
                .ToList();
            if (codeFiltered.Count > 0)
            {
                return codeFiltered.Count == 1
                    ? await OpenSingleChart(codeFiltered[0])
                    : await FormatChartList(airportId, codeFiltered);
            }
        }

        // Score every chart and disambiguate. The best match auto-opens
        // unless several matches are close in score, in which case we
        // show a scored picker.
        var scored = FuzzyMatcher.ScoreAll(
            filter,
            chartList,
            c => c.ChartName,
            c => ChartTypeBonus(filter, c));

        if (scored.Count == 0)
        {
            return CommandResult.FromError("No charts matched the filter.");
        }

        var (best, closeMatches) = FuzzyMatcher.Disambiguate(filter, scored);

        if (best is not null)
        {
            return await OpenSingleChart(best.Item);
        }

        return await FormatScoredPicker(airportId, closeMatches);
    }

    private async Task<CommandResult> OpenSingleChart(Charts.Models.Chart chart)
    {
        var url = await GetChartPdfUrl(chart);
        var text = $"  Opening: {TextFormatter.Colorize(chart.ChartName, AnsiColor.Green)}";
        return CommandResult.FromUrl(text, url);
    }

    private async Task<CommandResult> FormatChartList(string airportId, List<Charts.Models.Chart> chartList)
    {
        var sb = new StringBuilder();
        var grouped = chartList.GroupBy(c => c.ChartCode).OrderBy(g => ChartCodeOrder(g.Key));

        var index = 1;
        var selections = new Dictionary<int, Func<Task<CommandResult>>>();

        foreach (var group in grouped)
        {
            sb.AppendLine(TextFormatter.Colorize($"  [{group.Key}]", AnsiColor.Yellow));

            foreach (var chart in group.OrderBy(c => c.ChartName))
            {
                var num = TextFormatter.Colorize($"  {index,3})", AnsiColor.Cyan);
                sb.AppendLine($"{num} {chart.ChartName}");

                var url = await GetChartPdfUrl(chart);
                var name = chart.ChartName;
                selections[index] = () => Task.FromResult(
                    CommandResult.FromUrl(
                        $"  Opening: {TextFormatter.Colorize(name, AnsiColor.Green)}",
                        url));
                index++;
            }
        }

        sb.AppendLine();
        sb.AppendLine($"  {TextFormatter.Colorize($"{chartList.Count} charts for {airportId}", AnsiColor.Gray)} — enter a number to open");

        return new CommandResult(sb.ToString()) { PendingSelections = selections };
    }

    /// <summary>
    /// Picker for ambiguous fuzzy results — shows match scores so the user
    /// can see why each candidate is competing.
    /// </summary>
    private async Task<CommandResult> FormatScoredPicker(
        string airportId,
        IReadOnlyList<FuzzyMatcher.ScoredMatch<Charts.Models.Chart>> matches)
    {
        var sb = new StringBuilder();
        sb.AppendLine(TextFormatter.Colorize("  Multiple close matches — pick one:", AnsiColor.Gray));
        sb.AppendLine();

        var selections = new Dictionary<int, Func<Task<CommandResult>>>();
        var index = 1;

        foreach (var match in matches)
        {
            var chart = match.Item;
            var num = TextFormatter.Colorize($"  {index,3})", AnsiColor.Cyan);
            var code = TextFormatter.Colorize($"[{chart.ChartCode,-4}]", AnsiColor.Yellow);
            var score = TextFormatter.Colorize($"(score: {match.Score:F2})", AnsiColor.Gray);
            sb.AppendLine($"{num} {code} {chart.ChartName} {score}");

            var url = await GetChartPdfUrl(chart);
            var name = chart.ChartName;
            selections[index] = () => Task.FromResult(
                CommandResult.FromUrl(
                    $"  Opening: {TextFormatter.Colorize(name, AnsiColor.Green)}",
                    url));
            index++;
        }

        sb.AppendLine();
        sb.AppendLine($"  {TextFormatter.Colorize($"{matches.Count} close matches for {airportId}", AnsiColor.Gray)} — enter a number to open");

        return new CommandResult(sb.ToString()) { PendingSelections = selections };
    }

    private async Task<string> GetChartPdfUrl(Charts.Models.Chart chart)
    {
        int? scrollToPage = null;

        if (chart.ChartCode is "MIN" or "HOT" or "LAH")
        {
            scrollToPage = await chartPdfProcessingService.FindPageContainingFaaCode(chart);
        }

        return $"{chart.PdfUrl}#view=Fit&zoom=page-fit" + (scrollToPage is not null ? $"&page={scrollToPage}" : string.Empty);
    }

    private static int ChartCodeOrder(string code) => code.ToUpperInvariant() switch
    {
        "APD" => 0,
        "DP" => 1,
        "STAR" => 2,
        "IAP" => 3,
        _ => 4
    };

    /// <summary>
    /// Normalizes a chart search query so shorthand forms match real chart names.
    /// Mirrors <c>charts.py:_normalize_chart_name</c> in the standalone CLI.
    /// Steps (in order):
    ///   1. Whole-string aliases (<c>TAXI</c> → <c>AIRPORT DIAGRAM</c>, <c>DVA</c> → <c>DIVERSE VECTOR AREA</c>).
    ///   2. Navaid alias resolution (<c>FMG1</c> → <c>MUSTANG1</c>, <c>FMG FIVE</c> → <c>MUSTANG FIVE</c>).
    ///   3. SID/STAR digit suffix expansion (<c>DYAMD5</c> → <c>DYAMD FIVE</c>, <c>MUSTANG1</c> → <c>MUSTANG ONE</c>).
    ///   4. Runway zero-padding via <see cref="RunwayFormat.PadSingleDigit"/> so
    ///      <c>"ILS 4L"</c> matches stored chart names like <c>"ILS OR LOC RWY 04L"</c>.
    /// </summary>
    private async Task<string> NormalizeChartQuery(string rawFilter)
    {
        var trimmed = rawFilter.Trim();
        if (trimmed.Length == 0)
        {
            return rawFilter;
        }

        if (ChartNameAliases.TryGetValue(trimmed, out var aliased))
        {
            return aliased;
        }

        var upper = trimmed.ToUpperInvariant();
        var resolved = await nasrDataService.ResolveNavaidAlias(upper);

        var match = SidStarPatternRegex().Match(resolved);
        if (match.Success && DigitToWord.TryGetValue(match.Groups[2].Value, out var word))
        {
            resolved = $"{match.Groups[1].Value} {word}";
        }

        return RunwayFormat.PadSingleDigit(resolved);
    }

    /// <summary>
    /// Adds a small score bonus to charts whose ChartCode matches the type
    /// hinted by the query (e.g. "ILS" → IAP, "DEPARTURE" → DP). Helps break
    /// ambiguous fuzzy-match ties when the user gave us a type hint.
    /// </summary>
    private static double ChartTypeBonus(string query, Charts.Models.Chart chart)
    {
        var detected = DetectChartType(query);
        if (detected is null)
        {
            return 0;
        }
        return chart.ChartCode.Equals(detected, StringComparison.OrdinalIgnoreCase) ? 0.15 : 0;
    }

    private static string? DetectChartType(string query)
    {
        var tokens = FuzzyMatcher.TokenizeRegex()
            .Matches(query.ToUpperInvariant())
            .Select(m => m.Value)
            .ToHashSet(StringComparer.Ordinal);

        if (tokens.Overlaps(IapTriggers)) return "IAP";
        if (tokens.Overlaps(DpTriggers)) return "DP";
        if (tokens.Overlaps(StarTriggers)) return "STAR";
        if (tokens.Overlaps(ApdTriggers)) return "APD";
        return null;
    }

    [GeneratedRegex(@"^([A-Z]+)(\d)$")]
    private static partial Regex SidStarPatternRegex();

    public IEnumerable<string> GetCompletions(string partial, int argIndex)
    {
        if (argIndex <= 1)
        {
            return airportRepository.AllAirportIds
                .Where(id => id.StartsWith(partial, StringComparison.OrdinalIgnoreCase));
        }
        return [];
    }
}
