using System.Text;
using ZoaReference.Features.Docs.Repositories;
using ZoaReference.Features.Terminal.Services;

namespace ZoaReference.Features.Terminal.Commands;

public class ProcedureCommand(DocumentRepository documentRepository) : ITerminalCommand
{
    public string Name => "sop";
    public string[] Aliases => ["proc"];
    public string Summary => "Search and open procedures/documents";
    public string Usage => "sop              — List all document categories\n" +
                           "    sop <query>      — Search documents by name";

    public Task<CommandResult> ExecuteAsync(CommandArgs args)
    {
        if (args.Positional.Length == 0)
        {
            return Task.FromResult(ShowCategories());
        }

        var query = string.Join(" ", args.Positional);
        return Task.FromResult(SearchDocuments(query));
    }

    private CommandResult ShowCategories()
    {
        var categories = documentRepository.Categories.ToList();
        if (categories.Count == 0)
        {
            return new CommandResult(TextFormatter.FormatTableEmpty("Procedures", "No document categories loaded"));
        }

        var sb = new StringBuilder();
        var widths = new[] { 30, 10 };
        sb.Append(TextFormatter.FormatTableHeader("Document Categories", ["Category", "Count"], widths));

        foreach (var cat in categories)
        {
            sb.AppendLine(TextFormatter.FormatTableRow([cat.Name, cat.Documents.Count.ToString()], widths));
        }

        sb.AppendLine();
        sb.AppendLine($"  Type {TextFormatter.Colorize("sop <search>", AnsiColor.Cyan)} to search documents.");

        return CommandResult.FromText(sb.ToString());
    }

    private CommandResult SearchDocuments(string query)
    {
        var matches = documentRepository.Documents
            .Where(d => d.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
        {
            return CommandResult.FromError($"No documents found matching '{query}'");
        }

        if (matches.Count == 1)
        {
            var doc = matches[0];
            var text = $"  Opening: {TextFormatter.Colorize(doc.Name, AnsiColor.Green)}";
            return CommandResult.FromUrl(text, doc.Url);
        }

        var sb = new StringBuilder();
        var index = 1;
        var selections = new Dictionary<int, Func<Task<CommandResult>>>();

        foreach (var doc in matches)
        {
            var num = TextFormatter.Colorize($"  {index,3})", AnsiColor.Cyan);
            sb.AppendLine($"{num} {doc.Name}");

            var url = doc.Url;
            var name = doc.Name;
            selections[index] = () => Task.FromResult(
                CommandResult.FromUrl(
                    $"  Opening: {TextFormatter.Colorize(name, AnsiColor.Green)}",
                    url));
            index++;
        }

        sb.AppendLine();
        sb.AppendLine($"  {TextFormatter.Colorize($"{matches.Count} documents found", AnsiColor.Gray)} — enter a number to open");

        return new CommandResult(sb.ToString()) { PendingSelections = selections };
    }

    public IEnumerable<string> GetCompletions(string partial, int argIndex) => [];
}
