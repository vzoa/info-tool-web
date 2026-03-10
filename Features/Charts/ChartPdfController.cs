using Microsoft.AspNetCore.Mvc;
using ZoaReference.Features.Charts.Services;

namespace ZoaReference.Features.Charts;

[ApiController]
[Route("api/v1/charts")]
public class ChartPdfController(
    AviationApiChartService chartService,
    ChartPdfProcessingService pdfProcessingService) : ControllerBase
{
    /// <summary>
    /// Returns a processed PDF for the given airport chart: pages are rotation-corrected and
    /// merged into a single file. Falls back to a redirect to the raw FAA PDF if processing fails.
    /// Supports ETag-based caching for efficient revalidation.
    /// </summary>
    [HttpGet("{airportId}/{chartName}")]
    public async Task<IActionResult> GetProcessedPdf(
        string airportId,
        string chartName,
        CancellationToken ct)
    {
        var decodedChartName = Uri.UnescapeDataString(chartName);
        var charts = await chartService.GetChartsForId(airportId, ct);
        var chart = charts.FirstOrDefault(
            c => c.ChartName == decodedChartName);

        if (chart is null)
        {
            return NotFound();
        }

        var processed = await pdfProcessingService
            .GetProcessedPdf(chart, ct);

        if (processed is null)
        {
            var fallbackUrl = chart.Pages
                .OrderBy(p => p.PageNumber)
                .First()
                .PdfPath;
            return Redirect(fallbackUrl);
        }

        if (Request.Headers.IfNoneMatch == processed.ContentHash)
        {
            return StatusCode(304);
        }

        Response.Headers.ETag = processed.ContentHash;
        Response.Headers.CacheControl = "public, max-age=3600";

        return File(processed.PdfData, "application/pdf");
    }
}
