using Coravel.Scheduling.Schedule.Interfaces;
using ZoaReference.Features.Docs.Repositories;
using ZoaReference.Features.Docs.ScheduledJobs;
using ZoaReference.FeatureUtilities.Interfaces;

namespace ZoaReference.Features.Docs;

public class DocsModule : IServiceConfigurator, ISchedulerConfigurator
{
    public IServiceCollection AddServices(IServiceCollection services)
    {
        services.AddSingleton<DocumentRepository>();
        services.AddTransient<FetchAndStoreZoaDocs>();
        return services;
    }

    public Action<IScheduler> ConfigureScheduler()
    {
        return scheduler =>
        {
            scheduler.Schedule<FetchAndStoreZoaDocs>()
                .Hourly()
                .RunOnceAtStart();
        };
    }
}
