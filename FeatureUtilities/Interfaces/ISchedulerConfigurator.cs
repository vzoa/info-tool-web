using Coravel.Scheduling.Schedule.Interfaces;

namespace ZoaReference.FeatureUtilities.Interfaces;

public interface ISchedulerConfigurator
{
    public Action<IScheduler> ConfigureScheduler();
}
