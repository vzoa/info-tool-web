using System.Text;
using ZoaReference.Features.Nasr.Models;
using ZoaReference.Features.Nasr.Services;
using ZoaReference.Features.Terminal.Services;

namespace ZoaReference.Features.Terminal.Commands;

public class NavaidCommand(NasrDataService nasrDataService) : ITerminalCommand
{
    public string Name => "navaid";
    public string[] Aliases => ["nav"];
    public string Summary => "Search navaids by ID, name, or type";
    public string Usage => "navaid <query>\n" +
                           "    navaid SJC           — Search by navaid ID\n" +
                           "    navaid VORTAC        — Search by type\n" +
                           "    navaid SFO OAK RNO   — Look up multiple navaids at once";

    public async Task<CommandResult> ExecuteAsync(CommandArgs args)
    {
        if (args.Positional.Length < 1)
        {
            return CommandResult.FromError("Usage: navaid <query>");
        }

        // Multiple positional args → treat each as its own lookup (matches the CLI),
        // e.g. `navaid SFO OAK RNO`. A multi-word name still works with quotes.
        if (args.Positional.Length > 1)
        {
            return await LookupMultiple(args.Positional);
        }

        var query = args.Positional[0];
        var results = await nasrDataService.SearchNavaids(query);

        if (results.Count == 0)
        {
            return CommandResult.FromError($"No navaids found matching '{query}'");
        }

        return CommandResult.FromText(FormatResultsTable(query, results));
    }

    private async Task<CommandResult> LookupMultiple(string[] queries)
    {
        var sb = new StringBuilder();
        var notFound = new List<string>();
        var anyFound = false;

        foreach (var query in queries)
        {
            var results = await nasrDataService.SearchNavaids(query);

            if (results.Count == 0)
            {
                notFound.Add(query);
                continue;
            }

            if (anyFound)
            {
                sb.AppendLine();
            }
            sb.Append(FormatResultsTable(query, results));
            anyFound = true;
        }

        if (!anyFound)
        {
            return CommandResult.FromError(
                $"No navaids found matching: {string.Join(", ", notFound)}");
        }

        if (notFound.Count > 0)
        {
            sb.AppendLine();
            sb.AppendLine(TextFormatter.Colorize(
                $"  No matches for: {string.Join(", ", notFound)}", AnsiColor.Gray));
        }

        return CommandResult.FromText(sb.ToString());
    }

    private static string FormatResultsTable(string query, IReadOnlyList<NavaidInfo> results)
    {
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

        return sb.ToString();
    }

    private static string Truncate(string text, int maxLen) =>
        text.Length <= maxLen ? text : text[..(maxLen - 3)] + "...";

    public IEnumerable<string> GetCompletions(string partial, int argIndex) => [];
}
