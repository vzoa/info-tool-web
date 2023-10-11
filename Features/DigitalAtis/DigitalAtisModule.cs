using Coravel.Scheduling.Schedule.Interfaces;
using ZoaReference.Features.DigitalAtis.Repositories;
using ZoaReference.Features.DigitalAtis.ScheduledJobs;
using ZoaReference.FeatureUtilities.Interfaces;

namespace ZoaReference.Features.DigitalAtis;

public class DigitalAtisModule : IServiceConfigurator, ISchedulerConfigurator
{
    public IServiceCollection AddServices(IServiceCollection services)
    {
        services.AddSingleton<DigitalAtisRepository>();
        services.AddTransient<FetchAndStoreAtis>();
        return services;
    }

    public Action<IScheduler> ConfigureScheduler()
    {
        return scheduler =>
        {
            scheduler.Schedule<FetchAndStoreAtis>()
                .EveryMinute()
                .RunOnceAtStart();
        };
    }
}
