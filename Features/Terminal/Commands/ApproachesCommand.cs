using System.Text;
using ZoaReference.Features.Charts.Models;
using ZoaReference.Features.Charts.Services;
using ZoaReference.Features.Terminal.Services;

namespace ZoaReference.Features.Terminal.Commands;

public class ApproachesCommand(StarApproachConnectionService connectionService) : ITerminalCommand
{
    public string Name => "approaches";
    public string[] Aliases => ["apps"];
    public string Summary => "Find approach charts connected to a STAR or fix";
    public string Usage => "approaches <airport> <star|fix>\n" +
                           "    approaches OAK CNDEL5  — Approaches connected to CNDEL5 STAR\n" +
                           "    approaches OAK CNDEL   — Approaches via CNDEL fix";

    public async Task<CommandResult> ExecuteAsync(CommandArgs args)
    {
        if (args.Positional.Length < 2)
        {
            return CommandResult.FromError("Usage: approaches <airport> <star|fix>");
        }

        var airportId = AirportIdHelper.NormalizeToIcao(args.Positional[0]);
        var query = args.Positional[1].ToUpperInvariant();

        List<ApproachConnection> connections;
        string headerLabel;

        if (StarApproachConnectionService.IsStarName(query))
        {
            var (star, starConnections) = await connectionService.FindConnectionsForStar(airportId, query);
            if (star is null)
            {
                return CommandResult.FromError($"STAR '{query}' not found at {airportId}");
            }
            connections = starConnections;
            headerLabel = $"STAR {star.Identifier}";
        }
        else
        {
            connections = await connectionService.FindConnectionsForFix(airportId, query);
            headerLabel = $"Fix {query}";
        }

        if (connections.Count == 0)
        {
            return CommandResult.FromError($"No approach connections found for {headerLabel} at {airportId}");
        }

        var sb = new StringBuilder();
        var widths = new[] { 36, 14, 10, 10 };
        sb.Append(TextFormatter.FormatTableHeader(
            $"Approach Connections — {airportId} via {headerLabel}",
            ["Approach", "Fix", "Role", "Runway"], widths));

        foreach (var conn in connections)
        {
            var role = conn.FixType switch
            {
                FixRole.IAF => TextFormatter.Colorize("IAF", AnsiColor.Green),
                FixRole.IF => TextFormatter.Colorize("IF", AnsiColor.Yellow),
                FixRole.FAF => TextFormatter.Colorize("FAF", AnsiColor.Orange),
                FixRole.Feeder => TextFormatter.Colorize("Feeder", AnsiColor.Cyan),
                _ => "-"
            };
            sb.AppendLine($"  {conn.ApproachChartName.PadRight(widths[0])}{conn.ConnectingFix.PadRight(widths[1])}{role.PadRight(widths[2] + 10)}{conn.Runway ?? "-"}");
        }

        return CommandResult.FromText(sb.ToString());
    }

    public IEnumerable<string> GetCompletions(string partial, int argIndex) => [];
}
