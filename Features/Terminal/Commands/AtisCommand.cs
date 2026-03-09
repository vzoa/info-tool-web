using System.Text;
using ZoaReference.Features.DigitalAtis.Models;
using ZoaReference.Features.DigitalAtis.Repositories;
using ZoaReference.Features.Terminal.Services;

namespace ZoaReference.Features.Terminal.Commands;

public class AtisCommand(DigitalAtisRepository atisRepository) : ITerminalCommand
{
    public string Name => "atis";
    public string[] Aliases => [];
    public string Summary => "Display current ATIS information";
    public string Usage => "atis <airport>   — Show ATIS for a specific airport (e.g., atis SFO)\n" +
                           "    atis --all       — Show ATIS for all available airports";

    public Task<CommandResult> ExecuteAsync(CommandArgs args)
    {
        if (args.Flags.ContainsKey("all"))
        {
            return Task.FromResult(ShowAllAtis());
        }

        if (args.Positional.Length < 1)
        {
            return Task.FromResult(CommandResult.FromError("Usage: atis <airport> or atis --all"));
        }

        var airportId = AirportIdHelper.NormalizeToIcao(args.Positional[0]);
        return Task.FromResult(ShowAirportAtis(airportId));
    }

    private CommandResult ShowAirportAtis(string icaoId)
    {
        if (!atisRepository.TryGetAtisForId(icaoId, out var record))
        {
            return CommandResult.FromError($"No ATIS available for {icaoId}");
        }

        var sb = new StringBuilder();
        var widths = new[] { 10, 8, 10, 52 };
        sb.Append(TextFormatter.FormatTableHeader($"ATIS — {icaoId}", ["Type", "Info", "Altimeter", "Status"], widths));

        AppendAtisRow(sb, record.Combined, widths);
        AppendAtisRow(sb, record.Departure, widths);
        AppendAtisRow(sb, record.Arrival, widths);

        return CommandResult.FromText(sb.ToString());
    }

    private CommandResult ShowAllAtis()
    {
        var allAtis = atisRepository.GetAllAtis().ToList();
        if (allAtis.Count == 0)
        {
            return new CommandResult(TextFormatter.FormatTableEmpty("ATIS", "No ATIS data available"));
        }

        var sb = new StringBuilder();
        var widths = new[] { 8, 10, 8, 10, 44 };
        sb.Append(TextFormatter.FormatTableHeader("All ATIS", ["Airport", "Type", "Info", "Altimeter", "Weather"], widths));

        foreach (var atis in allAtis.OrderBy(a => a.IcaoId))
        {
            var type = atis.Type == Atis.AtisType.Combined ? "COMB" : atis.Type.ToString()[..3].ToUpperInvariant();
            sb.AppendLine(TextFormatter.FormatTableRow(
                [atis.IcaoId, type, atis.InfoLetter.ToString(), atis.Altimeter.ToString(), Truncate(atis.WeatherText, 42)],
                widths));
        }

        return CommandResult.FromText(sb.ToString());
    }

    private static void AppendAtisRow(StringBuilder sb, Atis? atis, int[] widths)
    {
        if (atis is null) return;

        var type = atis.Type == Atis.AtisType.Combined ? "COMB" : atis.Type.ToString()[..3].ToUpperInvariant();
        sb.AppendLine(TextFormatter.FormatTableRow(
            [type, atis.InfoLetter.ToString(), atis.Altimeter.ToString(), Truncate(atis.StatusText, 50)],
            widths));
    }

    private static string Truncate(string text, int maxLen) =>
        text.Length <= maxLen ? text : text[..(maxLen - 3)] + "...";

    public IEnumerable<string> GetCompletions(string partial, int argIndex)
    {
        if (argIndex <= 1)
        {
            return atisRepository.GetAllAtis()
                .Select(a => a.IcaoId)
                .Distinct()
                .Where(id => id.StartsWith(partial, StringComparison.OrdinalIgnoreCase));
        }
        return [];
    }
}
