using Coravel.Scheduling.Schedule.Interfaces;
using ZoaReference.Features.Nasr.ScheduledJobs;
using ZoaReference.Features.Nasr.Services;
using ZoaReference.FeatureUtilities.Interfaces;

namespace ZoaReference.Features.Nasr;

public class NasrFeature : IServiceConfigurator, ISchedulerConfigurator
{
    public IServiceCollection AddServices(IServiceCollection services)
    {
        services.AddSingleton<NasrDataService>();
        services.AddTransient<FetchNasrData>();
        return services;
    }

    public Action<IScheduler> ConfigureScheduler()
    {
        var rnd = new Random();
        return scheduler =>
        {
            scheduler.Schedule<FetchNasrData>()
                .DailyAt(7, rnd.Next(60))
                .RunOnceAtStart();
        };
    }
}
