using System.Text;
using ZoaReference.Features.Nasr.Services;
using ZoaReference.Features.Terminal.Services;

namespace ZoaReference.Features.Terminal.Commands;

public class MeaCommand(NasrDataService nasrDataService) : ITerminalCommand
{
    public string Name => "mea";
    public string[] Aliases => [];
    public string Summary => "Show MEA/MOCA between fixes on an airway";
    public string Usage => "mea <fix1> <fix2> [--a altitude]\n" +
                           "    mea SJC MOD         — Show MEA/MOCA for SJC to MOD\n" +
                           "    mea SJC MOD --a 50  — Highlight segments with MEA above 5000ft";

    public async Task<CommandResult> ExecuteAsync(CommandArgs args)
    {
        if (args.Positional.Length < 2)
        {
            return CommandResult.FromError("Usage: mea <fix1> <fix2> [--a altitude]");
        }

        var fix1 = args.Positional[0].ToUpperInvariant();
        var fix2 = args.Positional[1].ToUpperInvariant();

        int? alertAltitude = null;
        if (args.Flags.TryGetValue("a", out var altStr) && altStr is not null && int.TryParse(altStr, out var alt))
        {
            alertAltitude = alt;
        }

        var fix1Coords = await nasrDataService.GetWaypointCoordinates(fix1);
        var fix2Coords = await nasrDataService.GetWaypointCoordinates(fix2);

        if (fix1Coords is null)
        {
            return CommandResult.FromError($"Fix '{fix1}' not found in NASR data");
        }
        if (fix2Coords is null)
        {
            return CommandResult.FromError($"Fix '{fix2}' not found in NASR data");
        }

        // Find airways containing both fixes efficiently
        var fix1Airways = await nasrDataService.FindAirwaysContainingFix(fix1);
        var fix2Airways = await nasrDataService.FindAirwaysContainingFix(fix2);
        var fix2Set = new HashSet<string>(fix2Airways, StringComparer.OrdinalIgnoreCase);
        var commonAirways = fix1Airways.Where(a => fix2Set.Contains(a)).ToList();

        if (commonAirways.Count == 0)
        {
            return CommandResult.FromError($"No common airway found between {fix1} and {fix2}");
        }

        var sb = new StringBuilder();

        foreach (var airwayId in commonAirways)
        {
            var restrictions = await nasrDataService.GetAirwayRestrictions(airwayId);
            var relevant = restrictions
                .Where(r =>
                    r.FromFix.Equals(fix1, StringComparison.OrdinalIgnoreCase) ||
                    r.FromFix.Equals(fix2, StringComparison.OrdinalIgnoreCase) ||
                    r.ToFix.Equals(fix1, StringComparison.OrdinalIgnoreCase) ||
                    r.ToFix.Equals(fix2, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (relevant.Count == 0) continue;

            var widths = new[] { 12, 12, 10, 10 };
            sb.Append(TextFormatter.FormatTableHeader($"MEA/MOCA — {airwayId}: {fix1} → {fix2}",
                ["From", "To", "MEA", "MOCA"], widths));

            foreach (var r in relevant)
            {
                var mea = r.Mea.HasValue ? $"{r.Mea}00" : "-";
                var moca = r.Moca.HasValue ? $"{r.Moca}00" : "-";

                var isAboveAlert = alertAltitude.HasValue && r.Mea.HasValue && r.Mea.Value > alertAltitude.Value;
                if (isAboveAlert)
                {
                    mea = TextFormatter.Colorize(mea, AnsiColor.Red);
                }

                sb.AppendLine(TextFormatter.FormatTableRow([r.FromFix, r.ToFix, mea, moca], widths));
            }
        }

        if (sb.Length == 0)
        {
            return CommandResult.FromError($"No MEA/MOCA data found between {fix1} and {fix2}");
        }

        return CommandResult.FromText(sb.ToString());
    }

    public IEnumerable<string> GetCompletions(string partial, int argIndex) => [];
}
