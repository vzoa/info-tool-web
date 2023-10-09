using Coravel.Invocable;
using Microsoft.Extensions.Options;
using ZoaReference.Features.Charts.Services;

namespace ZoaReference.Features.Charts.ScheduledJobs;

public class FetchAndCacheCharts(ILogger<FetchAndCacheCharts> logger, AviationApiChartService chartService, IOptionsMonitor<AppSettings> appSettings) : IInvocable
{
    public async Task Invoke()
    {
        // In future we can replace with spread operator
        var airports = new List<string>();
        airports.AddRange(appSettings.CurrentValue.ArtccAirports.Bravos);
        airports.AddRange(appSettings.CurrentValue.ArtccAirports.Charlies);
        airports.AddRange(appSettings.CurrentValue.ArtccAirports.Deltas);

        // This method forces service to fetch and cache, and we can discard value
        logger.LogInformation("Fetching all charts");
        await chartService.GetChartsForIds(airports);
    }
}