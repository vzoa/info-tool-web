using System.Text;
using ZoaReference.Features.IcaoReference.Repositories;
using ZoaReference.Features.Terminal.Services;

namespace ZoaReference.Features.Terminal.Commands;

public class AircraftCommand(AircraftTypeRepository aircraftTypeRepository) : ITerminalCommand
{
    public string Name => "aircraft";
    public string[] Aliases => ["ac"];
    public string Summary => "Search aircraft by ICAO type code or manufacturer/model";
    public string Usage => "aircraft <query>\n" +
                           "    aircraft B738     — Search by ICAO type code\n" +
                           "    aircraft boeing   — Search by manufacturer/model";

    public Task<CommandResult> ExecuteAsync(CommandArgs args)
    {
        if (args.Positional.Length < 1)
        {
            return Task.FromResult(CommandResult.FromError("Usage: aircraft <query>"));
        }

        var query = string.Join(" ", args.Positional);
        var results = aircraftTypeRepository.AllAircraftTypes
            .Where(a =>
                a.IcaoId.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                a.Manufacturer.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                a.Model.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Take(25)
            .ToList();

        if (results.Count == 0)
        {
            return Task.FromResult(CommandResult.FromError($"No aircraft types found matching '{query}'"));
        }

        var sb = new StringBuilder();
        var widths = new[] { 8, 20, 24, 8, 8, 8 };
        sb.Append(TextFormatter.FormatTableHeader($"Aircraft Types — '{query}'",
            ["ICAO", "Manufacturer", "Model", "Class", "Wake", "Weight"], widths));

        foreach (var ac in results)
        {
            sb.AppendLine(TextFormatter.FormatTableRow(
                [ac.IcaoId, Truncate(ac.Manufacturer, 18), Truncate(ac.Model, 22),
                 ac.Class, ac.IcaoWakeTurbulenceCategory, ac.FaaWeightClass],
                widths));
        }

        return Task.FromResult(CommandResult.FromText(sb.ToString()));
    }

    private static string Truncate(string text, int maxLen) =>
        text.Length <= maxLen ? text : text[..(maxLen - 3)] + "...";

    public IEnumerable<string> GetCompletions(string partial, int argIndex)
    {
        if (argIndex <= 1)
        {
            return aircraftTypeRepository.AllAircraftTypes
                .Select(a => a.IcaoId)
                .Distinct()
                .Where(id => id.StartsWith(partial, StringComparison.OrdinalIgnoreCase))
                .Take(20);
        }
        return [];
    }
}
