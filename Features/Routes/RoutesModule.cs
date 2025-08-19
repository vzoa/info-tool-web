using Coravel.Scheduling.Schedule.Interfaces;
using ZoaReference.Features.Routes.Repositories;
using ZoaReference.Features.Routes.ScheduledJobs;
using ZoaReference.Features.Routes.Services;
using ZoaReference.FeatureUtilities.Interfaces;

namespace ZoaReference.Features.Routes;

public class RoutesModule : IServiceConfigurator, ISchedulerConfigurator
{
    public Action<IScheduler> ConfigureScheduler()
    {
        var rnd = new Random();
        return scheduler =>
        {
            scheduler.Schedule<FetchAndStoreAliasRoutes>()
                .HourlyAt(rnd.Next(60))
                .RunOnceAtStart();

            scheduler.Schedule<FetchAndStoreLoaRules>()
                .HourlyAt(rnd.Next(60))
                .RunOnceAtStart();
        };
    }

    public IServiceCollection AddServices(IServiceCollection services)
    {
        services.AddSingleton<FlightAwareRouteService>();
        services.AddSingleton<AliasRouteRuleRepository>();
        services.AddSingleton<LoaRuleRepository>();
        services.AddTransient<FetchAndStoreAliasRoutes>();
        services.AddTransient<FetchAndStoreLoaRules>();
        services.AddTransient<CskoRouteService>();
        return services;
    }
}
