using System.Text;
using ZoaReference.Features.Docs.Models;
using ZoaReference.Features.Docs.Repositories;
using ZoaReference.Features.Docs.Services;
using ZoaReference.Features.Terminal.Services;

namespace ZoaReference.Features.Terminal.Commands;

public class ProcedureCommand(
    DocumentRepository documentRepository,
    PdfSectionFinder sectionFinder) : ITerminalCommand
{
    private const string PdfViewerFragment = "#view=Fit&zoom=page-fit";

    public string Name => "sop";
    public string[] Aliases => ["proc"];
    public string Summary => "Search and open procedures/documents";
    public string Usage => "sop                          — List all document categories\n" +
                           "    sop <query>                  — Search documents by name\n" +
                           "    sop OAK                      — Open Oakland ATCT SOP\n" +
                           "    sop OAK 2-2                  — Open OAK SOP at section 2-2\n" +
                           "    sop SJC \"IFR Departures\"     — Open SJC SOP at section\n" +
                           "    sop SJC \"IFR Departures\" SJCE — Find SJCE in section\n" +
                           "    sop --list                   — List all document categories";

    public Task<CommandResult> ExecuteAsync(CommandArgs args)
    {
        if (args.Flags.ContainsKey("list") || args.Positional.Length == 0)
        {
            return Task.FromResult(ShowCategories());
        }

        var parsed = ProcedureQuery.Parse(args.Positional);
        if (string.IsNullOrWhiteSpace(parsed.ProcedureTerm))
        {
            return Task.FromResult(ShowCategories());
        }

        return Task.FromResult(SearchDocuments(parsed));
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

    private CommandResult SearchDocuments(ProcedureQuery query)
    {
        var (bestMatch, matches) = ProcedureMatcher.FindByName(
            documentRepository.Documents,
            query.ProcedureTerm);

        if (matches.Count == 0)
        {
            return CommandResult.FromError($"No documents found matching '{query.ProcedureTerm}'");
        }

        if (bestMatch is not null)
        {
            return OpenDocument(bestMatch.Value, query);
        }

        // Ambiguous — show numbered disambiguation list
        var sb = new StringBuilder();
        var index = 1;
        var selections = new Dictionary<int, Func<Task<CommandResult>>>();

        foreach (var match in matches)
        {
            var num = TextFormatter.Colorize($"  {index,3})", AnsiColor.Cyan);
            var score = TextFormatter.Colorize($"({match.Score:F2})", AnsiColor.Gray);
            sb.AppendLine($"{num} {match.Document.Name} {score}");

            var doc = match.Document;
            var q = query;
            selections[index] = () => Task.FromResult(OpenDocument(doc, q));
            index++;
        }

        sb.AppendLine();
        sb.AppendLine($"  {TextFormatter.Colorize($"{matches.Count} documents found", AnsiColor.Gray)} — enter a number to open");

        return new CommandResult(sb.ToString()) { PendingSelections = selections };
    }

    private CommandResult OpenDocument(Document doc, ProcedureQuery query)
    {
        var sb = new StringBuilder();
        var url = doc.Url;
        int? pageNum = null;

        if (query is { SectionTerm: not null, SearchTerm: not null })
        {
            pageNum = sectionFinder.FindTextInSection(
                doc.Url, query.SectionTerm, query.SearchTerm);
        }
        else if (query.SectionTerm is not null)
        {
            pageNum = sectionFinder.FindSectionPage(doc.Url, query.SectionTerm);
        }

        if (pageNum is not null)
        {
            url = $"{doc.Url}#page={pageNum}";
        }
        else
        {
            url = $"{doc.Url}{PdfViewerFragment}";
        }

        sb.Append($"  Opening: {TextFormatter.Colorize(doc.Name, AnsiColor.Green)}");

        if (query.SectionTerm is not null && pageNum is not null)
        {
            sb.Append($" — page {TextFormatter.Colorize(pageNum.Value.ToString(), AnsiColor.Cyan)}");
        }
        else if (query.SectionTerm is not null)
        {
            sb.Append($" — section '{TextFormatter.Colorize(query.SectionTerm, AnsiColor.Yellow)}' not found, opening first page");
        }

        return CommandResult.FromUrl(sb.ToString(), url);
    }

    public IEnumerable<string> GetCompletions(string partial, int argIndex) => [];
}
