using Coravel;
using ZoaReference.FeatureUtilities.Interfaces;

namespace ZoaReference.FeatureUtilities;

public static class ServiceExtensions
{
    public static IServiceCollection AddFeatureServices(this IServiceCollection collection)
    {
        var configurators = DiscoverClassesOfType<IServiceConfigurator>();
        foreach (var configurator in configurators)
        {
            configurator.AddServices(collection);
        }
        return collection;
    }
    
    public static IServiceProvider UseSchedulers(this IServiceProvider provider)
    {
        var configurators = DiscoverClassesOfType<ISchedulerConfigurator>();
        foreach (var configurator in configurators)
        {
            provider.UseScheduler(configurator.ConfigureScheduler());
        }
        return provider;
    }

    private static IEnumerable<T> DiscoverClassesOfType<T>()
    {
        return typeof(T).Assembly
            .GetTypes()
            .Where(p => p.IsClass && p.IsAssignableTo(typeof(T)))
            .Select(Activator.CreateInstance)
            .Cast<T>();
    }
}
