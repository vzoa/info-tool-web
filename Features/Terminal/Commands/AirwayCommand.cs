using System.Text;
using ZoaReference.Features.Nasr.Services;
using ZoaReference.Features.Terminal.Services;

namespace ZoaReference.Features.Terminal.Commands;

public class AirwayCommand(NasrDataService nasrDataService) : ITerminalCommand
{
    public string Name => "airway";
    public string[] Aliases => ["aw"];
    public string Summary => "Show fixes along an airway";
    public string Usage => "airway <id> [highlights...]\n" +
                           "    airway V25         — List all fixes along V25\n" +
                           "    airway V25 SJC MOD — Highlight SJC and MOD";

    public async Task<CommandResult> ExecuteAsync(CommandArgs args)
    {
        if (args.Positional.Length < 1)
        {
            return CommandResult.FromError("Usage: airway <id> [highlights...]");
        }

        var airwayId = args.Positional[0].ToUpperInvariant();
        var highlights = args.Positional.Length > 1
            ? args.Positional[1..].Select(h => h.ToUpperInvariant()).ToHashSet()
            : new HashSet<string>();

        var fixes = await nasrDataService.GetAirwayFixes(airwayId);

        if (fixes.Count == 0)
        {
            return CommandResult.FromError($"No airway found with ID '{airwayId}'");
        }

        var sb = new StringBuilder();
        var widths = new[] { 6, 12, 28 };
        sb.Append(TextFormatter.FormatTableHeader($"Airway {airwayId} — {fixes.Count} fixes",
            ["Seq", "Fix", "Coordinates"], widths));

        foreach (var fix in fixes)
        {
            var coords = $"{fix.Latitude:F4}, {fix.Longitude:F4}";
            var fixDisplay = highlights.Contains(fix.FixId.ToUpperInvariant())
                ? TextFormatter.Colorize(fix.FixId, AnsiColor.Orange)
                : fix.FixId;
            // Pad the non-highlighted version for alignment
            var seqStr = fix.Sequence.ToString();
            sb.Append("  ");
            sb.Append(seqStr.PadRight(widths[0]));
            sb.Append(highlights.Contains(fix.FixId.ToUpperInvariant())
                ? (fixDisplay + new string(' ', Math.Max(0, widths[1] - fix.FixId.Length)))
                : fix.FixId.PadRight(widths[1]));
            sb.AppendLine(coords);
        }

        // Show restrictions if available
        var restrictions = await nasrDataService.GetAirwayRestrictions(airwayId);
        if (restrictions.Count > 0)
        {
            sb.AppendLine();
            var rWidths = new[] { 12, 12, 10, 10 };
            sb.Append(TextFormatter.FormatTableHeader($"MEA/MOCA — {airwayId}",
                ["From", "To", "MEA", "MOCA"], rWidths));

            foreach (var r in restrictions)
            {
                var mea = r.Mea.HasValue ? $"{r.Mea}00" : "-";
                var moca = r.Moca.HasValue ? $"{r.Moca}00" : "-";
                sb.AppendLine(TextFormatter.FormatTableRow([r.FromFix, r.ToFix, mea, moca], rWidths));
            }
        }

        return CommandResult.FromText(sb.ToString());
    }

    public IEnumerable<string> GetCompletions(string partial, int argIndex) => [];
}
