using System.Text.RegularExpressions;
using ZoaReference.Features.Charts.Models;

namespace ZoaReference.Features.Charts.Services;

public partial class StarApproachConnectionService(
    CifpService cifpService,
    AviationApiChartService chartService,
    ILogger<StarApproachConnectionService> logger)
{
    private static readonly Dictionary<string, string[]> ApproachTypePrefixes = new()
    {
        ["RNAV"] = ["H", "R"],
        ["GPS"] = ["H", "R"],
        ["RNP"] = ["H", "R"],
        ["ILS"] = ["I"],
        ["LOC"] = ["L"],
        ["VOR/DME"] = ["D"],
        ["VOR"] = ["V"],
        ["NDB"] = ["N"],
        ["TACAN"] = ["T"],
        ["LDA"] = ["X"],
        ["SDF"] = ["U"],
    };

    public async Task<(CifpStarData? Star, List<ApproachConnection> Connections)>
        FindConnectionsForStar(string airportId, string starName, CancellationToken ct = default)
    {
        var starData = await cifpService.GetStarData(airportId, starName, ct);
        if (starData is null)
        {
            return (null, []);
        }

        var charts = await chartService.GetChartsForId(airportId, ct);
        var iapCharts = charts
            .Where(c => c.ChartCode == "IAP" && !c.ChartName.Contains("CONT."))
            .ToList();

        var approaches = await cifpService.GetApproachesForAirport(airportId, ct);
        var starWaypoints = new HashSet<string>(starData.Waypoints);
        var connections = new List<ApproachConnection>();

        foreach (var iapChart in iapCharts)
        {
            var matchedApproach = MatchChartToApproach(iapChart.ChartName, approaches);
            if (matchedApproach is null)
            {
                continue;
            }

            var iafSet = new HashSet<string>(matchedApproach.IafFixes);
            var ifSet = new HashSet<string>(matchedApproach.IfFixes);

            var iafConnections = starWaypoints.Intersect(iafSet).ToList();
            foreach (var fix in iafConnections)
            {
                connections.Add(new ApproachConnection(
                    iapChart.ChartName, fix, FixRole.IAF,
                    ExtractRunway(iapChart.ChartName)));
            }

            var ifConnections = starWaypoints.Intersect(ifSet).Except(iafConnections).ToList();
            foreach (var fix in ifConnections)
            {
                connections.Add(new ApproachConnection(
                    iapChart.ChartName, fix, FixRole.IF,
                    ExtractRunway(iapChart.ChartName)));
            }

            var feederFixes = new HashSet<string>(matchedApproach.FeederFixes);
            var feederConnections = starWaypoints
                .Intersect(feederFixes)
                .Except(iafConnections)
                .Except(ifConnections)
                .ToList();

            foreach (var fix in feederConnections)
            {
                connections.Add(new ApproachConnection(
                    iapChart.ChartName, fix, FixRole.Feeder,
                    ExtractRunway(iapChart.ChartName)));
            }
        }

        connections.Sort((a, b) =>
        {
            var cmp = string.Compare(a.Runway ?? "", b.Runway ?? "", StringComparison.Ordinal);
            return cmp != 0 ? cmp : string.Compare(a.ApproachChartName, b.ApproachChartName, StringComparison.Ordinal);
        });

        return (starData, connections);
    }

    public async Task<List<ApproachConnection>> FindConnectionsForFix(
        string airportId, string fixName, CancellationToken ct = default)
    {
        fixName = fixName.ToUpperInvariant().Trim();

        var charts = await chartService.GetChartsForId(airportId, ct);
        var iapCharts = charts
            .Where(c => c.ChartCode == "IAP" && !c.ChartName.Contains("CONT."))
            .ToList();

        var approaches = await cifpService.GetApproachesForAirport(airportId, ct);
        var connections = new List<ApproachConnection>();

        foreach (var iapChart in iapCharts)
        {
            var matchedApproach = MatchChartToApproach(iapChart.ChartName, approaches);
            if (matchedApproach is null)
            {
                continue;
            }

            if (matchedApproach.IafFixes.Contains(fixName))
            {
                connections.Add(new ApproachConnection(
                    iapChart.ChartName, fixName, FixRole.IAF,
                    ExtractRunway(iapChart.ChartName)));
            }
            else if (matchedApproach.IfFixes.Contains(fixName))
            {
                connections.Add(new ApproachConnection(
                    iapChart.ChartName, fixName, FixRole.IF,
                    ExtractRunway(iapChart.ChartName)));
            }
            else if (matchedApproach.FeederFixes.Contains(fixName))
            {
                connections.Add(new ApproachConnection(
                    iapChart.ChartName, fixName, FixRole.Feeder,
                    ExtractRunway(iapChart.ChartName)));
            }
        }

        connections.Sort((a, b) => string.Compare(a.ApproachChartName, b.ApproachChartName, StringComparison.Ordinal));
        return connections;
    }

    private static CifpApproach? MatchChartToApproach(
        string chartName, Dictionary<string, CifpApproach> approaches)
    {
        var chartUpper = chartName.ToUpperInvariant();
        var runway = ExtractRunway(chartName);
        if (runway is null)
        {
            return null;
        }

        string[] typePrefixes = [];
        foreach (var (keyword, prefixes) in ApproachTypePrefixes)
        {
            if (chartUpper.Contains(keyword))
            {
                typePrefixes = prefixes;
                break;
            }
        }

        char? chartVariant = null;
        foreach (var v in "XYZW")
        {
            if (chartUpper.Contains($" {v} ") || chartUpper.EndsWith($" {v}"))
            {
                chartVariant = v;
                break;
            }
        }

        CifpApproach? bestMatch = null;

        foreach (var (approachId, approach) in approaches)
        {
            if (approach.Runway != runway)
            {
                continue;
            }

            if (typePrefixes.Length > 0 && !typePrefixes.Any(p => approachId.StartsWith(p)))
            {
                continue;
            }

            char? approachVariant = null;
            if (approachId.Length > 0 && "XYZW".Contains(approachId[^1]))
            {
                approachVariant = approachId[^1];
            }

            if (chartVariant.HasValue)
            {
                if (approachVariant == chartVariant.Value)
                {
                    return approach;
                }

                continue;
            }

            bestMatch ??= approach;
        }

        return bestMatch;
    }

    private static string? ExtractRunway(string chartName)
    {
        var match = RunwayRegex().Match(chartName);
        return match.Success ? match.Groups[1].Value : null;
    }

    public async Task<List<string>> GetAutocompleteSuggestions(
        string airportId, CancellationToken ct = default)
    {
        var starNames = await cifpService.GetStarNamesForAirport(airportId, ct);
        var approaches = await cifpService.GetApproachesForAirport(airportId, ct);

        var entryFixes = new HashSet<string>();
        foreach (var approach in approaches.Values)
        {
            foreach (var fix in approach.IafFixes)
            {
                entryFixes.Add(fix);
            }

            foreach (var fix in approach.IfFixes)
            {
                entryFixes.Add(fix);
            }

            foreach (var fix in approach.FeederFixes)
            {
                entryFixes.Add(fix);
            }
        }

        var suggestions = new List<string>();
        suggestions.AddRange(starNames);
        suggestions.AddRange(entryFixes.OrderBy(f => f));
        return suggestions;
    }

    public static bool IsStarName(string name)
    {
        return StarNameRegex().IsMatch(name.ToUpperInvariant().Trim());
    }

    [GeneratedRegex(@"RWY\s+(\d{1,2}[LRC]?)")]
    private static partial Regex RunwayRegex();

    [GeneratedRegex(@"^[A-Z]+\d$")]
    private static partial Regex StarNameRegex();
}
