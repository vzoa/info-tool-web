using ZoaReference.Features.Terminal.Commands;
using ZoaReference.Features.Terminal.Services;
using ZoaReference.FeatureUtilities.Interfaces;

namespace ZoaReference.Features.Terminal;

public class TerminalModule : IServiceConfigurator
{
    public IServiceCollection AddServices(IServiceCollection services)
    {
        // Register all commands as singletons
        services.AddSingleton<ITerminalCommand, HelpCommand>();
        services.AddSingleton<ITerminalCommand, DescentCommand>();
        services.AddSingleton<ITerminalCommand, AtisCommand>();
        services.AddSingleton<ITerminalCommand, ChartCommand>();
        services.AddSingleton<ITerminalCommand, RouteCommand>();
        services.AddSingleton<ITerminalCommand, AirlineCommand>();
        services.AddSingleton<ITerminalCommand, AirportCommand>();
        services.AddSingleton<ITerminalCommand, AircraftCommand>();
        services.AddSingleton<ITerminalCommand, ScratchpadCommand>();
        services.AddSingleton<ITerminalCommand, ProcedureCommand>();
        services.AddSingleton<ITerminalCommand, PositionCommand>();
        services.AddSingleton<ITerminalCommand, AirportsCommand>();
        services.AddSingleton<ITerminalCommand, OpenCommand>();
        services.AddSingleton<ITerminalCommand, ApproachesCommand>();
        services.AddSingleton<ITerminalCommand, NavaidCommand>();
        services.AddSingleton<ITerminalCommand, AirwayCommand>();
        services.AddSingleton<ITerminalCommand, MeaCommand>();
        services.AddSingleton<ITerminalCommand, CifpCommand>();
        services.AddSingleton<ITerminalCommand, ListCommand>();
        services.AddSingleton<ITerminalCommand, ClearCommand>();
        services.AddSingleton<ITerminalCommand, CloseCommand>();

        // CommandDispatcher is scoped (one per circuit)
        services.AddScoped<CommandDispatcher>();

        return services;
    }
}
