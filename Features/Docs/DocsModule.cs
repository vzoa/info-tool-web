using Coravel.Scheduling.Schedule.Interfaces;
using ZoaReference.Features.Docs.Repositories;
using ZoaReference.Features.Docs.ScheduledJobs;
using ZoaReference.Features.Docs.Services;
using ZoaReference.FeatureUtilities.Interfaces;

namespace ZoaReference.Features.Docs;

public class DocsModule : IServiceConfigurator, ISchedulerConfigurator
{
    public IServiceCollection AddServices(IServiceCollection services)
    {
        services.AddSingleton<DocumentRepository>();
        services.AddSingleton<PdfSectionFinder>();
        services.AddSingleton<ProcedureSearchConfig>();
        services.AddSingleton<ProcedureMatcher>();
        services.AddTransient<FetchAndStoreDocs>();
        return services;
    }

    public Action<IScheduler> ConfigureScheduler()
    {
        return scheduler =>
        {
            scheduler.Schedule<FetchAndStoreDocs>()
                .Hourly()
                .RunOnceAtStart();
        };
    }
}
