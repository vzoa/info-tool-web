using System.Text;
using Microsoft.Extensions.Options;
using ZoaReference.Features.Terminal.Services;

namespace ZoaReference.Features.Terminal.Commands;

public class AirportsCommand(IOptionsMonitor<AppSettings> appSettings) : ITerminalCommand
{
    public string Name => "airports";
    public string[] Aliases => [];
    public string Summary => "List configured ARTCC airports";
    public string Usage => "airports  — Show all configured ARTCC airports by class";

    public Task<CommandResult> ExecuteAsync(CommandArgs args)
    {
        var settings = appSettings.CurrentValue.ArtccAirports;
        var sb = new StringBuilder();

        sb.AppendLine(TextFormatter.Colorize("  ARTCC Airports", AnsiColor.Orange));
        sb.AppendLine();

        AppendGroup(sb, "Bravo", settings.Bravos);
        AppendGroup(sb, "Charlie", settings.Charlies);
        AppendGroup(sb, "Delta", settings.Deltas);
        AppendGroup(sb, "Other", settings.Other);

        return Task.FromResult(CommandResult.FromText(sb.ToString()));
    }

    private static void AppendGroup(StringBuilder sb, string label, ICollection<string> airports)
    {
        if (airports.Count == 0) return;
        sb.AppendLine($"  {TextFormatter.Colorize(label, AnsiColor.Yellow)}: {string.Join(", ", airports)}");
    }

    public IEnumerable<string> GetCompletions(string partial, int argIndex) => [];
}
