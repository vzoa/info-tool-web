using Coravel.Scheduling.Schedule.Interfaces;
using ZoaReference.Features.Routes.Repositories;
using ZoaReference.Features.Routes.ScheduledJobs;
using ZoaReference.Features.Routes.Services;
using ZoaReference.FeatureUtilities.Interfaces;

namespace ZoaReference.Features.Routes;

public class RoutesModule : IServiceConfigurator, ISchedulerConfigurator
{
    public IServiceCollection AddServices(IServiceCollection services)
    {
        services.AddSingleton<FlightAwareRouteService>();
        services.AddSingleton<AliasRouteRuleRepository>();
        services.AddTransient<FetchAndStoreAliasRoutes>();
        return services;
    }
    
    public Action<IScheduler> ConfigureScheduler()
    {
        var rnd = new Random();
        return scheduler =>
        {
            scheduler.Schedule<FetchAndStoreAliasRoutes>()
                .HourlyAt(rnd.Next(60))
                .RunOnceAtStart();
        };
    }
}
