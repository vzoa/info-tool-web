using System.Text;
using ZoaReference.Features.Nasr.Services;
using ZoaReference.Features.Terminal.Services;

namespace ZoaReference.Features.Terminal.Commands;

public class MeaCommand(NasrDataService nasrDataService) : ITerminalCommand
{
    public string Name => "mea";
    public string[] Aliases => [];
    public string Summary => "Show MEA/MOCA between fixes on an airway";
    public string Usage => "mea <fix1> <fix2> [-a altitude]\n" +
                           "    mea SJC MOD        — Show MEA/MOCA for SJC to MOD\n" +
                           "    mea SJC MOD -a 50  — Highlight segments with MEA above 5000ft and show SAFE/WARNING";

    public async Task<CommandResult> ExecuteAsync(CommandArgs args)
    {
        if (args.Positional.Length < 2)
            return CommandResult.FromError("Usage: mea <fix1> <fix2> [-a altitude]");

        var fix1 = args.Positional[0].ToUpperInvariant();
        var fix2 = args.Positional[1].ToUpperInvariant();

        // Accept both -a and --a (and --altitude for good measure)
        int? alertAltitude = null;
        foreach (var key in new[] { "a", "altitude" })
        {
            if (args.Flags.TryGetValue(key, out var altStr) && altStr is not null && int.TryParse(altStr, out var alt))
            {
                alertAltitude = alt;
                break;
            }
        }

        var fix1Coords = await nasrDataService.GetWaypointCoordinates(fix1);
        var fix2Coords = await nasrDataService.GetWaypointCoordinates(fix2);

        if (fix1Coords is null)
            return CommandResult.FromError($"Fix '{fix1}' not found in NASR data");
        if (fix2Coords is null)
            return CommandResult.FromError($"Fix '{fix2}' not found in NASR data");

        var fix1Airways = await nasrDataService.FindAirwaysContainingFix(fix1);
        var fix2Airways = await nasrDataService.FindAirwaysContainingFix(fix2);
        var fix2Set = new HashSet<string>(fix2Airways, StringComparer.OrdinalIgnoreCase);
        var commonAirways = fix1Airways.Where(a => fix2Set.Contains(a)).ToList();

        if (commonAirways.Count == 0)
            return CommandResult.FromError($"No common airway found between {fix1} and {fix2}");

        // Collect all relevant segments across all common airways
        var allSegments = new List<(string Airway, string From, string To, int Mea, int? Moca)>();

        foreach (var airwayId in commonAirways)
        {
            var restrictions = await nasrDataService.GetAirwayRestrictions(airwayId);
            foreach (var r in restrictions)
            {
                if ((r.FromFix.Equals(fix1, StringComparison.OrdinalIgnoreCase) ||
                     r.FromFix.Equals(fix2, StringComparison.OrdinalIgnoreCase) ||
                     r.ToFix.Equals(fix1, StringComparison.OrdinalIgnoreCase) ||
                     r.ToFix.Equals(fix2, StringComparison.OrdinalIgnoreCase)) &&
                    r.Mea.HasValue)
                {
                    allSegments.Add((airwayId, r.FromFix, r.ToFix, r.Mea!.Value * 100,
                        r.Moca.HasValue ? r.Moca.Value * 100 : null));
                }
            }
        }

        if (allSegments.Count == 0)
            return CommandResult.FromError($"No MEA/MOCA data found between {fix1} and {fix2}");

        // Sort segments by MEA descending (highest restriction first)
        allSegments.Sort((a, b) => b.Mea.CompareTo(a.Mea));

        var maxMea = allSegments[0].Mea;
        var alertAltFt = alertAltitude.HasValue ? alertAltitude.Value * 100 : (int?)null;
        var sb = new StringBuilder();

        // Safety status or max MEA summary
        if (alertAltFt.HasValue)
        {
            var isSafe = alertAltFt.Value >= maxMea;
            if (isSafe)
            {
                sb.AppendLine(TextFormatter.Colorize(
                    $"SAFE: {alertAltFt.Value:N0} ft meets MEA requirement of {maxMea:N0} ft",
                    AnsiColor.Green));
            }
            else
            {
                sb.AppendLine(TextFormatter.Colorize(
                    $"WARNING: {alertAltFt.Value:N0} ft is BELOW required MEA of {maxMea:N0} ft",
                    AnsiColor.Yellow));
            }
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine($"Maximum MEA: {maxMea:N0} ft");
            sb.AppendLine();
        }

        // Segment table, grouped by airway
        foreach (var airwayId in commonAirways)
        {
            var airwaySegs = allSegments.Where(s => s.Airway == airwayId).ToList();
            if (airwaySegs.Count == 0) continue;

            var widths = new[] { 12, 12, 10, 10 };
            sb.Append(TextFormatter.FormatTableHeader($"MEA/MOCA — {airwayId}: {fix1} → {fix2}",
                ["From", "To", "MEA", "MOCA"], widths));

            foreach (var (_, from, to, mea, moca) in airwaySegs)
            {
                var meaStr = $"{mea:N0}";
                var mocaStr = moca.HasValue ? $"{moca.Value:N0}" : "-";

                var isAboveAlert = alertAltFt.HasValue && mea > alertAltFt.Value;
                if (isAboveAlert)
                    meaStr = TextFormatter.Colorize(meaStr, AnsiColor.Red);

                sb.AppendLine(TextFormatter.FormatTableRow([from, to, meaStr, mocaStr], widths));
            }
        }

        return CommandResult.FromText(sb.ToString());
    }

    public IEnumerable<string> GetCompletions(string partial, int argIndex) => [];
}
