using Coravel.Invocable;
using Microsoft.Extensions.Options;
using ZoaReference.Features.Charts.ScheduledJobs;
using ZoaReference.Features.Charts.Services;
using ZoaReference.Features.VnasData.Services;

namespace ZoaReference.Features.VnasData.ScheduledJobs;

public class FetchAndCacheVnasData(ILogger<FetchAndCacheCharts> logger, CachedVnasDataService vnasDataService) : IInvocable
{
    public async Task Invoke()
    {
        var artcc = appSettings.Value.ARTCC;

        // Use the dynamic value for logging and the service call
        logger.LogInformation("Fetching all VNAS Data for {Artcc}", artcc);
        await vnasDataService.ForceCache(artcc);
    }
}