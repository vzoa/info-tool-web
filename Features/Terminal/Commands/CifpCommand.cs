using System.Text;
using ZoaReference.Features.Charts.Models;
using ZoaReference.Features.Charts.Services;
using ZoaReference.Features.Terminal.Services;

namespace ZoaReference.Features.Terminal.Commands;

public class CifpCommand(CifpService cifpService) : ITerminalCommand
{
    public string Name => "cifp";
    public string[] Aliases => [];
    public string Summary => "Show CIFP procedure details (SID/STAR/approach legs)";
    public string Usage => "cifp <airport> <procedure>\n" +
                           "    cifp KSFO ILS28R       — ILS 28R approach legs\n" +
                           "    cifp KSFO DYAMD5.CMPMN — STAR with specific transition\n" +
                           "    cifp KSFO OFFSH8       — SID procedure legs";

    public async Task<CommandResult> ExecuteAsync(CommandArgs args)
    {
        if (args.Positional.Length < 2)
        {
            return CommandResult.FromError("Usage: cifp <airport> <procedure>");
        }

        var airport = AirportIdHelper.NormalizeToIcao(args.Positional[0]);
        var procedureName = string.Join(" ", args.Positional[1..]);

        var detail = await cifpService.GetProcedureDetail(airport, procedureName);
        if (detail is null)
        {
            return CommandResult.FromError($"Procedure '{procedureName}' not found at {airport}");
        }

        var sb = new StringBuilder();

        // Header
        var typeLabel = detail.ProcedureType switch
        {
            CifpProcedureType.SID => "SID",
            CifpProcedureType.STAR => "STAR",
            CifpProcedureType.Approach => "Approach",
            _ => "Procedure"
        };

        sb.AppendLine(TextFormatter.Colorize($"  {typeLabel}: {detail.Name} — {detail.Airport}", AnsiColor.Orange));
        sb.AppendLine();

        // Legs table
        var widths = new[] { 8, 8, 8, 12, 8, 8, 8 };
        sb.Append(TextFormatter.FormatTableHeader(
            $"Procedure Legs ({detail.Legs.Count})",
            ["Fix", "Term", "Role", "Altitude", "Speed", "Course", "Dist"], widths));

        foreach (var leg in detail.Legs)
        {
            var role = leg.Role switch
            {
                FixRole.IAF => TextFormatter.Colorize("IAF", AnsiColor.Green),
                FixRole.IF => TextFormatter.Colorize("IF", AnsiColor.Yellow),
                FixRole.FAF => TextFormatter.Colorize("FAF", AnsiColor.Orange),
                _ => ""
            };

            var alt = leg.AltitudeConstraint ?? "";
            var spd = leg.SpeedConstraint?.ToString() ?? "";
            var crs = leg.Course.HasValue ? $"{leg.Course:F0}°" : "";
            var dist = leg.Distance.HasValue ? $"{leg.Distance:F1}" : "";

            sb.AppendLine($"  {leg.FixId.PadRight(widths[0])}{leg.PathTerminator.PadRight(widths[1])}{(role.Length > 0 ? role + new string(' ', Math.Max(0, widths[2] - 3)) : "".PadRight(widths[2]))}{alt.PadRight(widths[3])}{spd.PadRight(widths[4])}{crs.PadRight(widths[5])}{dist}");
        }

        // Route diagram
        sb.AppendLine();
        sb.AppendLine(DrawHorizontalRoute(detail));

        return CommandResult.FromText(sb.ToString());
    }

    private static string DrawHorizontalRoute(CifpProcedureDetail detail)
    {
        var fixes = detail.Legs
            .Where(l => !string.IsNullOrEmpty(l.FixId))
            .Select(l => l.FixId)
            .Distinct()
            .ToList();

        if (fixes.Count == 0) return "";

        var sb = new StringBuilder();
        sb.Append("  ");

        for (var i = 0; i < fixes.Count; i++)
        {
            var fix = fixes[i];
            var leg = detail.Legs.FirstOrDefault(l => l.FixId == fix);

            var color = leg?.Role switch
            {
                FixRole.IAF => AnsiColor.Green,
                FixRole.IF => AnsiColor.Yellow,
                FixRole.FAF => AnsiColor.Orange,
                _ => AnsiColor.White
            };

            sb.Append(TextFormatter.Colorize(fix, color));

            if (i < fixes.Count - 1)
            {
                sb.Append(TextFormatter.Colorize(" ─── ", AnsiColor.Gray));
            }
        }

        return sb.ToString();
    }

    public IEnumerable<string> GetCompletions(string partial, int argIndex) => [];
}
