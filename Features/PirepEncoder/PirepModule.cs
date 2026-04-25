using ZoaReference.FeatureUtilities.Interfaces;

namespace ZoaReference.Features.PirepEncoder;

public class PirepModule : IServiceConfigurator
{
    public IServiceCollection AddServices(IServiceCollection services) => services;
}
