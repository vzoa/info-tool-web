using System.Text;
using ZoaReference.Features.Nasr.Services;
using ZoaReference.Features.Terminal.Services;

namespace ZoaReference.Features.Terminal.Commands;

public class NavaidCommand(NasrDataService nasrDataService) : ITerminalCommand
{
    public string Name => "navaid";
    public string[] Aliases => ["nav"];
    public string Summary => "Search navaids by ID, name, or type";
    public string Usage => "navaid <query>\n" +
                           "    navaid SJC     — Search by navaid ID\n" +
                           "    navaid VORTAC  — Search by type";

    public async Task<CommandResult> ExecuteAsync(CommandArgs args)
    {
        if (args.Positional.Length < 1)
        {
            return CommandResult.FromError("Usage: navaid <query>");
        }

        var query = string.Join(" ", args.Positional);
        var results = await nasrDataService.SearchNavaids(query);

        if (results.Count == 0)
        {
            return CommandResult.FromError($"No navaids found matching '{query}'");
        }

        var displayed = results.Take(30).ToList();
        var sb = new StringBuilder();
        var widths = new[] { 8, 24, 12, 10, 24 };
        sb.Append(TextFormatter.FormatTableHeader($"Navaids — '{query}'",
            ["ID", "Name", "Type", "Freq", "Coordinates"], widths));

        foreach (var nav in displayed)
        {
            var coords = $"{nav.Latitude:F4}, {nav.Longitude:F4}";
            sb.AppendLine(TextFormatter.FormatTableRow(
                [nav.Id, Truncate(nav.Name, 22), nav.Type, nav.Frequency, coords],
                widths));
        }

        if (results.Count > 30)
        {
            sb.AppendLine($"  {TextFormatter.Colorize($"... and {results.Count - 30} more", AnsiColor.Gray)}");
        }

        return CommandResult.FromText(sb.ToString());
    }

    private static string Truncate(string text, int maxLen) =>
        text.Length <= maxLen ? text : text[..(maxLen - 3)] + "...";

    public IEnumerable<string> GetCompletions(string partial, int argIndex) => [];
}
