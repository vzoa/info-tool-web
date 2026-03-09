using System.Text;
using ZoaReference.Features.Scratchpads.Repositories;
using ZoaReference.Features.Terminal.Services;

namespace ZoaReference.Features.Terminal.Commands;

public class ScratchpadCommand(ScratchpadsRepository scratchpadsRepository) : ITerminalCommand
{
    public string Name => "scratchpad";
    public string[] Aliases => ["scratch"];
    public string Summary => "Look up scratchpad entries for a facility";
    public string Usage => "scratchpad <facility>\n" +
                           "    scratchpad NCT   — Show NorCal TRACON scratchpads\n" +
                           "    scratchpad SFO   — Show SFO scratchpads";

    public Task<CommandResult> ExecuteAsync(CommandArgs args)
    {
        if (args.Positional.Length < 1)
        {
            return Task.FromResult(CommandResult.FromError("Usage: scratchpad <facility>"));
        }

        var facilityId = args.Positional[0].ToUpperInvariant();

        if (!scratchpadsRepository.TryGetValue(facilityId, out var scratchpads) || scratchpads is null)
        {
            // Try ICAO normalization
            var icao = AirportIdHelper.NormalizeToIcao(facilityId);
            if (!scratchpadsRepository.TryGetValue(icao, out scratchpads) || scratchpads is null)
            {
                return Task.FromResult(CommandResult.FromError($"No scratchpads found for '{facilityId}'"));
            }
        }

        if (scratchpads.Count == 0)
        {
            return Task.FromResult(new CommandResult(
                TextFormatter.FormatTableEmpty($"Scratchpads — {facilityId}", "No scratchpad entries")));
        }

        var sb = new StringBuilder();
        var widths = new[] { 16, 64 };
        sb.Append(TextFormatter.FormatTableHeader($"Scratchpads — {facilityId}", ["Entry", "Description"], widths));

        foreach (var sp in scratchpads)
        {
            sb.AppendLine(TextFormatter.FormatTableRow([sp.Entry, sp.Description], widths));
        }

        return Task.FromResult(CommandResult.FromText(sb.ToString()));
    }

    public IEnumerable<string> GetCompletions(string partial, int argIndex)
    {
        if (argIndex <= 1)
        {
            return scratchpadsRepository.AllAirportIds
                .Where(id => id.StartsWith(partial, StringComparison.OrdinalIgnoreCase));
        }
        return [];
    }
}
