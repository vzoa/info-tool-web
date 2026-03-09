using System.Text;
using ZoaReference.Features.Terminal.Services;
using ZoaReference.Features.VnasData.Models;
using ZoaReference.Features.VnasData.Services;

namespace ZoaReference.Features.Terminal.Commands;

public class PositionCommand(CachedVnasDataService vnasDataService) : ITerminalCommand
{
    public string Name => "position";
    public string[] Aliases => ["pos"];
    public string Summary => "Search ATC positions by name, callsign, or radio name";
    public string Usage => "position <query>\n" +
                           "    position NorCal  — Search positions matching 'NorCal'\n" +
                           "    position TWR     — Search tower positions";

    public async Task<CommandResult> ExecuteAsync(CommandArgs args)
    {
        if (args.Positional.Length < 1)
        {
            return CommandResult.FromError("Usage: position <query>");
        }

        var query = string.Join(" ", args.Positional);
        var facilities = await vnasDataService.GetArtccFacilities("ZOA");

        var positions = facilities
            .SelectMany(f => f.Facility.Positions?.Select(p =>
                new PositionExtended(p, f.Facility.StarsConfiguration?.Tcps?
                    .FirstOrDefault(t => t.Id == p.StarsConfiguration?.TcpId)?.Id)) ?? [])
            .Where(p =>
                p.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                p.Callsign.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                p.RadioName.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                p.Tcp.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                FormatFrequency(p.Frequency).Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(30)
            .ToList();

        if (positions.Count == 0)
        {
            return CommandResult.FromError($"No positions found matching '{query}'");
        }

        var sb = new StringBuilder();
        var widths = new[] { 24, 10, 16, 16, 12 };
        sb.Append(TextFormatter.FormatTableHeader($"Positions — '{query}'",
            ["Name", "TCP", "Callsign", "Radio Name", "Frequency"], widths));

        foreach (var pos in positions)
        {
            var freq = pos.Frequency > 0 ? FormatFrequency(pos.Frequency) : "-";
            sb.AppendLine(TextFormatter.FormatTableRow(
                [Truncate(pos.Name, 22), pos.Tcp, pos.Callsign, pos.RadioName, freq],
                widths));
        }

        return CommandResult.FromText(sb.ToString());
    }

    private static string FormatFrequency(int freq)
    {
        return $"{freq / 1000000.0:F3}";
    }

    private static string Truncate(string text, int maxLen) =>
        text.Length <= maxLen ? text : text[..(maxLen - 3)] + "...";

    public IEnumerable<string> GetCompletions(string partial, int argIndex) => [];
}
