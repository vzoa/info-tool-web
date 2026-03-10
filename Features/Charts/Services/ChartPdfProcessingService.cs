using System.Security.Cryptography;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using Sentry;
using ZoaReference.Features.Charts.Models;

namespace ZoaReference.Features.Charts.Services;

public class ChartPdfProcessingService(
    IHttpClientFactory httpClientFactory,
    IMemoryCache cache,
    ILogger<ChartPdfProcessingService> logger,
    IOptionsMonitor<AppSettings> appSettings,
    PdfRotationDetector rotationDetector)
{
    /// <summary>
    /// Downloads all pages of a chart PDF, detects and corrects rotation using text orientation,
    /// merges multi-page charts into a single PDF, and caches the result.
    /// </summary>
    public async Task<ProcessedChart?> GetProcessedPdf(
        Chart chart,
        CancellationToken ct = default)
    {
        var cacheKey = $"ProcessedPdf:{chart.IcaoIdent}:{chart.ChartName}:{chart.ChartSeq}";
        if (cache.TryGetValue<ProcessedChart>(cacheKey, out var cached))
        {
            return cached;
        }

        try
        {
            var pages = chart.Pages.OrderBy(p => p.PageNumber).ToList();
            var client = httpClientFactory.CreateClient();

            var downloadTasks = pages
                .Select(p => client.GetByteArrayAsync(p.PdfPath, ct))
                .ToList();
            var pdfBytesList = await Task.WhenAll(downloadTasks);

            using var outputDoc = new PdfDocument();

            foreach (var pdfBytes in pdfBytesList)
            {
                var rotation = rotationDetector.DetectRotation(pdfBytes);

                using var inputStream = new MemoryStream(pdfBytes);
                using var inputDoc = PdfReader.Open(
                    inputStream, PdfDocumentOpenMode.Import);

                foreach (var page in inputDoc.Pages)
                {
                    var importedPage = outputDoc.AddPage(page);
                    if (rotation != 0)
                    {
                        importedPage.Rotate =
                            (importedPage.Rotate + rotation + 360) % 360;
                    }
                }
            }

            using var outputStream = new MemoryStream();
            outputDoc.Save(outputStream, false);
            var resultBytes = outputStream.ToArray();

            var hash = Convert.ToHexString(
                SHA256.HashData(resultBytes)).ToLowerInvariant();
            var result = new ProcessedChart(resultBytes, hash);

            var expiration = DateTimeOffset.UtcNow.AddSeconds(
                appSettings.CurrentValue.CacheTtls.Charts);
            cache.Set(cacheKey, result, expiration);

            return result;
        }
        catch (Exception ex)
        {
            SentrySdk.CaptureException(ex);
            logger.LogWarning(
                ex,
                "Failed to process PDF for chart {Chart} at {Airport}",
                chart.ChartName,
                chart.IcaoIdent);
            return null;
        }
    }
}
