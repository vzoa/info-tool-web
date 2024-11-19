using ZoaReference.FeatureUtilities.Interfaces;

namespace ZoaReference.Features.Healthcheck;

public class HealthcheckModule : IServiceConfigurator
{
    public IServiceCollection AddServices(IServiceCollection services)
    {
        return services;
    }
}