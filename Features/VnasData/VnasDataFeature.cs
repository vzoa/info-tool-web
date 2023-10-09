using Coravel.Scheduling.Schedule.Interfaces;
using ZoaReference.FeatureUtilities.Interfaces;
using ZoaReference.Features.VnasData.ScheduledJobs;
using ZoaReference.Features.VnasData.Services;

namespace ZoaReference.Features.VnasData;

public class VnasDataFeature : IServiceConfigurator, ISchedulerConfigurator
{
    public IServiceCollection AddServices(IServiceCollection services)
    {
        services.AddSingleton<CachedVnasDataService>();
        services.AddTransient<FetchAndCacheVnasData>();
        return services;
    }

    public Action<IScheduler> ConfigureScheduler()
    {
        var rnd = new Random();
        return scheduler =>
        {
            scheduler.Schedule<FetchAndCacheVnasData>()
                .HourlyAt(rnd.Next(60))
                .RunOnceAtStart();
        };
    }
}
