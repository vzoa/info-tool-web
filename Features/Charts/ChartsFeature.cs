using Coravel.Scheduling.Schedule.Interfaces;
using ZoaReference.Features.Charts.ScheduledJobs;
using ZoaReference.FeatureUtilities.Interfaces;
using ZoaReference.Features.Charts.Services;

namespace ZoaReference.Features.Charts;

public class ChartsFeature : IServiceConfigurator, ISchedulerConfigurator
{
    public IServiceCollection AddServices(IServiceCollection services)
    {
        services.AddSingleton<AviationApiChartService>();
        services.AddTransient<FetchAndCacheCharts>();
        return services;
    }

    public Action<IScheduler> ConfigureScheduler()
    {
        var rnd = new Random();
        return scheduler =>
        {
            scheduler.Schedule<FetchAndCacheCharts>()
                .HourlyAt(rnd.Next(60))
                .RunOnceAtStart();
        };
    }
}
