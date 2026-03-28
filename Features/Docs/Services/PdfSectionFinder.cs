using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Outline;

namespace ZoaReference.Features.Docs.Services;

/// <summary>
/// Finds section page numbers within locally-cached procedure PDFs.
/// </summary>
public partial class PdfSectionFinder(
    IWebHostEnvironment webHostEnvironment,
    IOptionsMonitor<AppSettings> appSettings,
    ILogger<PdfSectionFinder> logger)
{
    /// <summary>
    /// Find the 1-based page number for a section within the PDF at the given remote URL.
    /// Returns null if the section cannot be found or the PDF is unavailable.
    /// </summary>
    public int? FindSectionPage(string documentUrl, string sectionQuery)
    {
        var pdfBytes = LoadLocalPdf(documentUrl);
        if (pdfBytes is null)
        {
            return null;
        }

        return FindPageFromBytes(pdfBytes, sectionQuery);
    }

    public int? FindPageFromBytes(byte[] pdfBytes, string sectionQuery)
    {
        return FindPageFromBookmarks(pdfBytes, sectionQuery)
               ?? FindPageFromText(pdfBytes, sectionQuery);
    }

    /// <summary>
    /// Find the 1-based page number where searchTerm appears within a section.
    /// Falls back to just the section page if the search term isn't found.
    /// Returns null if neither can be found.
    /// </summary>
    public int? FindTextInSection(
        string documentUrl,
        string sectionQuery,
        string searchTerm)
    {
        var pdfBytes = LoadLocalPdf(documentUrl);
        if (pdfBytes is null)
        {
            return null;
        }

        var sectionPage = FindPageFromBookmarks(pdfBytes, sectionQuery)
                          ?? FindPageFromText(pdfBytes, sectionQuery);

        if (sectionPage is null)
        {
            return null;
        }

        var textPage = SearchTextFromPage(pdfBytes, sectionPage.Value, searchTerm);
        return textPage ?? sectionPage;
    }

    private byte[]? LoadLocalPdf(string documentUrl)
    {
        try
        {
            var fileName = Path.GetFileName(new Uri(documentUrl).AbsolutePath);
            var localPath = Path.ChangeExtension(
                Path.Combine(
                    webHostEnvironment.WebRootPath,
                    appSettings.CurrentValue.DocumentsPdfPath,
                    fileName),
                ".pdf");

            if (!File.Exists(localPath))
            {
                logger.LogDebug("Local PDF not found at {Path}", localPath);
                return null;
            }

            return File.ReadAllBytes(localPath);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to load local PDF for {Url}", documentUrl);
            return null;
        }
    }

    private int? FindPageFromBookmarks(byte[] pdfBytes, string sectionQuery)
    {
        try
        {
            using var stream = new MemoryStream(pdfBytes);
            using var document = PdfDocument.Open(stream);

            if (!document.TryGetBookmarks(out var bookmarks))
            {
                return null;
            }

            var nodes = bookmarks.GetNodes().ToList();
            if (nodes.Count == 0)
            {
                return null;
            }

            return FindMatchingBookmark(nodes, sectionQuery);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to extract bookmarks from PDF");
            return null;
        }
    }

    private static int? FindMatchingBookmark(
        List<BookmarkNode> nodes,
        string query)
    {
        var queryUpper = query.ToUpperInvariant();
        var docNodes = nodes.OfType<DocumentBookmarkNode>().ToList();

        if (docNodes.Count == 0)
        {
            return null;
        }

        // Check for section number pattern like "2-2" or "2.2"
        var sectionMatch = SectionNumberPattern().Match(query);
        if (sectionMatch.Success)
        {
            var flexPattern = new Regex(
                $@"\b{sectionMatch.Groups[1].Value}[-.\s]*{sectionMatch.Groups[2].Value}\b",
                RegexOptions.IgnoreCase);

            foreach (var node in docNodes)
            {
                if (flexPattern.IsMatch(node.Title))
                {
                    return node.PageNumber;
                }
            }
        }

        // Direct substring match
        foreach (var node in docNodes)
        {
            if (node.Title.Contains(queryUpper, StringComparison.OrdinalIgnoreCase))
            {
                return node.PageNumber;
            }
        }

        // All query words present in heading
        var queryWords = queryUpper.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (queryWords.Length > 1)
        {
            foreach (var node in docNodes)
            {
                var titleUpper = node.Title.ToUpperInvariant();
                if (queryWords.All(w => titleUpper.Contains(w, StringComparison.OrdinalIgnoreCase)))
                {
                    return node.PageNumber;
                }
            }
        }

        return null;
    }

    private int? FindPageFromText(byte[] pdfBytes, string sectionQuery)
    {
        try
        {
            using var stream = new MemoryStream(pdfBytes);
            using var document = PdfDocument.Open(stream);

            var queryUpper = sectionQuery.ToUpperInvariant();

            var sectionMatch = SectionNumberPattern().Match(sectionQuery);
            Regex? sectionPattern = null;
            if (sectionMatch.Success)
            {
                sectionPattern = new Regex(
                    $@"\b{sectionMatch.Groups[1].Value}[-.\s]*{sectionMatch.Groups[2].Value}\b",
                    RegexOptions.IgnoreCase);
            }

            foreach (var page in document.GetPages())
            {
                var text = page.Text ?? "";
                var textUpper = text.ToUpperInvariant();

                if (sectionPattern is not null && sectionPattern.IsMatch(text))
                {
                    return page.Number;
                }

                if (textUpper.Contains(queryUpper, StringComparison.Ordinal))
                {
                    return page.Number;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to search PDF text for section");
        }

        return null;
    }

    private int? SearchTextFromPage(byte[] pdfBytes, int startPage, string searchTerm)
    {
        try
        {
            using var stream = new MemoryStream(pdfBytes);
            using var document = PdfDocument.Open(stream);

            var searchUpper = searchTerm.ToUpperInvariant();

            for (var pageNum = startPage; pageNum <= document.NumberOfPages; pageNum++)
            {
                var page = document.GetPage(pageNum);
                var text = (page.Text ?? "").ToUpperInvariant();
                if (text.Contains(searchUpper, StringComparison.Ordinal))
                {
                    return pageNum;
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to search PDF text from page {Page}", startPage);
        }

        return null;
    }

    [GeneratedRegex(@"^(\d+)[-.](\d+)$")]
    private static partial Regex SectionNumberPattern();
}
