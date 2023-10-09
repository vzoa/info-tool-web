using ZoaReference.Features.DigitalAtis.Repositories;
using ZoaReference.Features.DigitalAtis.Services;
using ZoaReference.FeatureUtilities.Interfaces;

namespace ZoaReference.Features.DigitalAtis;

public class DigitalAtisModule : IServiceConfigurator
{
    public IServiceCollection AddServices(IServiceCollection services)
    {
        services.AddSingleton<DigitalAtisRepository>();
        services.AddHostedService<DigitalAtisBackgroundService>();
        return services;
    }
}
