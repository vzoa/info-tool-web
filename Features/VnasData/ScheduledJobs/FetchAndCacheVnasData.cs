using Coravel.Invocable;
using Microsoft.Extensions.Options;
using ZoaReference.Features.Charts.ScheduledJobs;
using ZoaReference.Features.Charts.Services;
using ZoaReference.Features.VnasData.Services;

namespace ZoaReference.Features.VnasData.ScheduledJobs;

public class FetchAndCacheVnasData(
    ILogger<FetchAndCacheVnasData> logger, 
    CachedVnasDataService vnasDataService,
    IOptionsMonitor<AppSettings> appSettings) : IInvocable
{
    public async Task Invoke()
    {
        var artcc = appSettings.CurrentValue.ARTCC;

        // Use the dynamic value for logging and the service call
        logger.LogInformation("Fetching all VNAS Data for {Artcc}", artcc);
        await vnasDataService.ForceCache(artcc);
    }
}