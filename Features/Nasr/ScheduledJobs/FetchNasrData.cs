using Coravel.Invocable;
using ZoaReference.Features.Nasr.Services;

namespace ZoaReference.Features.Nasr.ScheduledJobs;

public class FetchNasrData(NasrDataService nasrDataService) : IInvocable
{
    public async Task Invoke()
    {
        await nasrDataService.FetchAndCacheData();
    }
}
