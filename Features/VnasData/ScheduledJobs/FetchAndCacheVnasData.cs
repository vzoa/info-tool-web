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
        // This method forces service to fetch and cache, and we can discard value
        logger.LogInformation("Fetching all VNAS Data for ZOA");
        await vnasDataService.ForceCache("ZOA");
    }
}