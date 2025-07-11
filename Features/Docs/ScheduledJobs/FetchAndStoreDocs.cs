using Coravel.Invocable;
using Microsoft.Extensions.Options;
using ZoaReference.Features.Docs.Models;
using ZoaReference.Features.Docs.Repositories;

namespace ZoaReference.Features.Docs.ScheduledJobs;

public class FetchAndStoreDocs(ILogger<FetchAndStoreDocs> logger, IHttpClientFactory httpClientFactory, IWebHostEnvironment webHostEnvironment, IOptionsMonitor<AppSettings> appSettings, DocumentRepository documentRepository) : IInvocable
{
    public async Task Invoke()
    {
        var client = httpClientFactory.CreateClient();
        List<DocumentCategory> compiledDocCategories = [];

        try
        {
            logger.LogInformation("Fetching ZOA docs from {url}",
                appSettings.CurrentValue.Urls.ZoaDocumentsApiEndpoint);
            var fetchedDocCategories = await client.GetFromJsonAsync<List<ZoaDocumentCategory>>(appSettings.CurrentValue.Urls.ZoaDocumentsApiEndpoint);
            if (fetchedDocCategories is not null)
            {
                compiledDocCategories.AddRange(fetchedDocCategories.Select(c => c.ToGenericDocumentCategory()));
            }
            else
            {
                logger.LogInformation("Fetched ZOA documents null or zero");
            }

            var customDocCategories = appSettings.CurrentValue.CustomDocuments;
            compiledDocCategories.AddRange(customDocCategories.Select(c => c.ToGenericDocumentCategory()));

            logger.LogInformation("Successfully fetched ZOA and custom docs");
        }
        catch (Exception e)
        {
            logger.LogError("Error while fetching ZOA docs: {ex}", e.ToString());
        }

        var tasks = new List<Task>();
        foreach (var category in compiledDocCategories)
        {
            foreach (var doc in category.Documents)
            {
                var pdfName = GetPdfNameFromUrl(doc.Url);
                var localPdfPath = Path.ChangeExtension(Path.Combine(PdfFolderPath, pdfName), ".pdf");

                // Always write new file
                try
                {
                    var task = WriteRemotePdfToLocal(doc.Url, localPdfPath);
                    tasks.Add(task);
                    logger.LogInformation("Found pdf at {url} and writing at {path}", doc.Url, localPdfPath);
                }
                catch (Exception e)
                {
                    logger.LogWarning("Could not write PDF from {url} at {path}. Error: {e}", doc.Url, localPdfPath, e);
                }
            }
        }

        await Task.WhenAll(tasks);
        documentRepository.ClearAllDocumentCategories();
        documentRepository.AddDocumentCategories(compiledDocCategories);
    }

    private string PdfFolderPath => Path.Combine(webHostEnvironment.WebRootPath, appSettings.CurrentValue.DocumentsPdfPath);

    private static string GetPdfNameFromUrl(string url)
    {
        var uri = new Uri(url);
        return Path.GetFileName(uri.AbsolutePath);
    }

    private async Task WriteRemotePdfToLocal(string url, string path)
    {
        try
        {
            var client = httpClientFactory.CreateClient();
            await using var pdfStream = await client.GetStreamAsync(url);

            var dirPath = Path.GetDirectoryName(path);
            if (dirPath is not null && !Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            await using var pdfNewFile = File.Create(path);
            await pdfStream.CopyToAsync(pdfNewFile);
            logger.LogInformation("Wrote new PDF to {path}", path);
        }
        catch (Exception e)
        {
            logger.LogError("Error while fetching PDF from {url}: {ex}", url, e);
        }

    }
}