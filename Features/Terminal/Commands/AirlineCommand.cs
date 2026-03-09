using System.Text;
using ZoaReference.Features.IcaoReference.Repositories;
using ZoaReference.Features.Terminal.Services;

namespace ZoaReference.Features.Terminal.Commands;

public class AirlineCommand(AirlineRepository airlineRepository) : ITerminalCommand
{
    public string Name => "airline";
    public string[] Aliases => ["al"];
    public string Summary => "Search airline by ICAO code, callsign, or name";
    public string Usage => "airline <query>\n" +
                           "    airline UAL    — Search by ICAO code\n" +
                           "    airline united  — Search by name";

    public Task<CommandResult> ExecuteAsync(CommandArgs args)
    {
        if (args.Positional.Length < 1)
        {
            return Task.FromResult(CommandResult.FromError("Usage: airline <query>"));
        }

        var query = string.Join(" ", args.Positional);
        var results = airlineRepository.AllAirlines
            .Where(a =>
                (a.IcaoId?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (a.Callsign?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (a.Name?.Contains(query, StringComparison.OrdinalIgnoreCase) ?? false))
            .Take(25)
            .ToList();

        if (results.Count == 0)
        {
            return Task.FromResult(CommandResult.FromError($"No airlines found matching '{query}'"));
        }

        var sb = new StringBuilder();
        var widths = new[] { 8, 16, 36, 20 };
        sb.Append(TextFormatter.FormatTableHeader($"Airlines — '{query}'", ["ICAO", "Callsign", "Name", "Country"], widths));

        foreach (var airline in results)
        {
            sb.AppendLine(TextFormatter.FormatTableRow(
                [airline.IcaoId ?? "", airline.Callsign ?? "", airline.Name ?? "", airline.Country ?? ""],
                widths));
        }

        return Task.FromResult(CommandResult.FromText(sb.ToString()));
    }

    public IEnumerable<string> GetCompletions(string partial, int argIndex)
    {
        if (argIndex <= 1)
        {
            return airlineRepository.AllAirlines
                .Where(a => a.IcaoId != null)
                .Select(a => a.IcaoId!)
                .Where(id => id.StartsWith(partial, StringComparison.OrdinalIgnoreCase))
                .Take(20);
        }
        return [];
    }
}
