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

        // Filter by chart code if provided
        if (args.Positional.Length >= 2)
        {
            var rawFilter = string.Join(" ", args.Positional[1..]);
            // Normalize the filter so shorthand queries (TAXI, FMG1, DYAMD5, RNAV 4L)
            // match real chart names like "AIRPORT DIAGRAM", "MUSTANG ONE",
            // "DYAMD FIVE", "RNAV (GPS) RWY 04L".
            var filter = await NormalizeChartQuery(rawFilter);
            var codeFilter = args.Positional[1].ToUpperInvariant();

            // Try chart code first (DP, STAR, IAP, APD, etc.)
            var codeFiltered = chartList.Where(c =>
                c.ChartCode.Equals(codeFilter, StringComparison.OrdinalIgnoreCase)).ToList();

            if (codeFiltered.Count > 0 && args.Positional.Length == 2)
            {
                chartList = codeFiltered;
            }
            else
            {
                // Try exact substring match first
                var exactMatches = chartList.Where(c =>
                    c.ChartName.Contains(filter, StringComparison.OrdinalIgnoreCase)).ToList();

                // Fall back to token-based fuzzy matching
                chartList = exactMatches.Count > 0
                    ? exactMatches
                    : chartList.Where(c => FuzzyMatchChart(filter, c.ChartName)).ToList();
            }
        }

        if (chartList.Count == 0)
        {
            return CommandResult.FromError("No charts matched the filter.");
        }

        // Single result — open directly
        if (chartList.Count == 1)
        {
            var chart = chartList[0];
            var url = await GetChartPdfUrl(chart);
            var text = $"  Opening: {TextFormatter.Colorize(chart.ChartName, AnsiColor.Green)}";
            return CommandResult.FromUrl(text, url);
        }

        // Multiple results — show numbered list
        return await FormatChartList(airportId, chartList);
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

        // Step 1: whole-string aliases (check before uppercasing so the dict's
        // OrdinalIgnoreCase comparer handles the user's original casing).
        if (ChartNameAliases.TryGetValue(trimmed, out var aliased))
        {
            return aliased;
        }

        // Step 2: navaid alias resolution
        var upper = trimmed.ToUpperInvariant();
        var resolved = await nasrDataService.ResolveNavaidAlias(upper);

        // Step 3: single-token IDENT+DIGIT → IDENT + DIGIT_WORD
        var match = SidStarPatternRegex().Match(resolved);
        if (match.Success && DigitToWord.TryGetValue(match.Groups[2].Value, out var word))
        {
            resolved = $"{match.Groups[1].Value} {word}";
        }

        // Step 4: pad single-digit runway numbers so "4L" hits "04L" in chart names.
        return RunwayFormat.PadSingleDigit(resolved);
    }

    [GeneratedRegex(@"^([A-Z]+)(\d)$")]
    private static partial Regex SidStarPatternRegex();

    /// <summary>
    /// Token-based fuzzy match: splits query like "dyamd5" into alpha+digit parts,
    /// expands trailing digits to word form (5→FIVE), and checks if all tokens
    /// match chart name tokens by prefix or exact match.
    /// </summary>
    private static bool FuzzyMatchChart(string query, string chartName)
    {
        var queryUpper = query.ToUpperInvariant();
        var chartUpper = chartName.ToUpperInvariant();

        // Split query into tokens: "dyamd5" → ["DYAMD", "5"], "ils28r" → ["ILS", "28", "R"]
        var queryTokens = TokenizeRegex().Matches(queryUpper)
            .Select(m => m.Value).ToList();

        // Expand digit tokens to word equivalents: "5" → also match "FIVE"
        var expandedQueryTokens = new List<List<string>>();
        foreach (var token in queryTokens)
        {
            var alternatives = new List<string> { token };
            if (DigitToWord.TryGetValue(token, out var word))
            {
                alternatives.Add(word);
            }
            expandedQueryTokens.Add(alternatives);
        }

        var chartTokens = TokenizeRegex().Matches(chartUpper)
            .Select(m => m.Value).ToList();

        // Every query token (or its digit-word expansion) must match at least one
        // chart token as a prefix (query token is prefix of chart token)
        return expandedQueryTokens.All(alternatives =>
            alternatives.Any(alt =>
                chartTokens.Any(ct =>
                    ct.StartsWith(alt, StringComparison.Ordinal))));
    }

    [GeneratedRegex(@"[A-Z]+|\d+")]
    private static partial Regex TokenizeRegex();

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
