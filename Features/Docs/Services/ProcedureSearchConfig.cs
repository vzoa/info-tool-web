using Microsoft.Extensions.Options;

namespace ZoaReference.Features.Docs.Services;

public class ProcedureSearchConfig
{
    public HashSet<string> ClassDAirports { get; }
    public HashSet<string> AirportCodes { get; }
    public Dictionary<string, string> AirportAliases { get; }
    public Dictionary<string, string[]> ProcedureAliases { get; }

    public ProcedureSearchConfig(IOptions<AppSettings> options)
    {
        var settings = options.Value;
        var airports = settings.ArtccAirports;

        ClassDAirports = airports.Deltas
            .Select(StripIcaoPrefix)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        AirportCodes = airports.Bravos
            .Concat(airports.Charlies)
            .Concat(airports.Deltas)
            .Select(StripIcaoPrefix)
            .Concat(airports.Other)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        AirportAliases = new Dictionary<string, string>(
            settings.AirportAliases, StringComparer.OrdinalIgnoreCase);

        ProcedureAliases = new Dictionary<string, string[]>(
            settings.ProcedureAliases, StringComparer.OrdinalIgnoreCase);
    }

    private static string StripIcaoPrefix(string code)
    {
        return code.StartsWith('K') && code.Length == 4 ? code[1..] : code;
    }
}
