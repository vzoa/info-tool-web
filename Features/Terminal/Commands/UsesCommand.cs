using System.Text;
using ZoaReference.Features.Charts.Models;
using ZoaReference.Features.Charts.Services;
using ZoaReference.Features.Terminal.Services;

namespace ZoaReference.Features.Terminal.Commands;

public class UsesCommand(CifpService cifpService) : ITerminalCommand
{
    public string Name => "uses";
    public string[] Aliases => [];
    public string Summary => "Find procedures that contain a fix across all airports";
    public string Usage => "uses <fix> [airport] [type]\n" +
                           "    uses MYJAW           — All procedures containing MYJAW\n" +
                           "    uses COREZ BUR       — Only procedures at BUR\n" +
                           "    uses COREZ SID       — Only SIDs\n" +
                           "    uses COREZ BUR SID   — Only SIDs at BUR\n" +
                           "    uses FIX KAPP        — Airport APP (use ICAO to disambiguate)";

    public async Task<CommandResult> ExecuteAsync(CommandArgs args)
    {
        if (args.Positional.Length < 1)
        {
            return CommandResult.FromError("Usage: uses <fix> [airport] [type]");
        }

        var fix = args.Positional[0].ToUpperInvariant().Trim();
        var (airportFilter, typeFilter) = ParseFilters(args.Positional[1..]);

        var procedures = await cifpService.FindProceduresUsingFix(fix, airportFilter, typeFilter);

        if (procedures.Count == 0)
        {
            var filters = new List<string>();
            if (airportFilter is not null) filters.Add($"airport={airportFilter}");
            if (typeFilter is not null) filters.Add($"type={typeFilter}");
            var suffix = filters.Count > 0 ? $" ({string.Join(", ", filters)})" : "";
            return CommandResult.FromError($"No procedures found containing '{fix}'{suffix}");
        }

        return Format(fix, procedures);
    }

    /// <summary>
    /// Classifies each extra positional argument as either an airport filter
    /// or a procedure-type filter. Matches the CLI's <c>parse_uses_filters</c>
    /// heuristic: 4-letter K-prefix codes (e.g. <c>KAPP</c>, <c>KSFO</c>) are
    /// always airports, even if the 3-letter suffix collides with a type
    /// keyword. Anything that doesn't look like a type keyword becomes the
    /// airport filter.
    /// </summary>
    private static (string? AirportFilter, CifpProcedureType? TypeFilter) ParseFilters(string[] extras)
    {
        string? airportFilter = null;
        CifpProcedureType? typeFilter = null;

        foreach (var arg in extras)
        {
            var upper = arg.ToUpperInvariant();
            var isIcao = upper.Length == 4 && upper.StartsWith('K');

            if (!isIcao)
            {
                CifpProcedureType? type = upper switch
                {
                    "SID" => CifpProcedureType.SID,
                    "STAR" => CifpProcedureType.STAR,
                    "APP" or "IAP" or "APPROACH" => CifpProcedureType.Approach,
                    _ => null
                };
                if (type is not null)
                {
                    typeFilter = type;
                    continue;
                }
            }

            // Airport filter: strip K prefix so the filter matches the 3-letter
            // form that CifpService uses internally.
            airportFilter = isIcao ? upper[1..] : upper;
        }

        return (airportFilter, typeFilter);
    }

    private static CommandResult Format(string fix, IReadOnlyList<CifpFixUsage> procedures)
    {
        var byAirport = procedures
            .GroupBy(p => p.Airport, StringComparer.OrdinalIgnoreCase)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine($"  {TextFormatter.Colorize(fix, AnsiColor.Yellow)} is used in:");
        sb.AppendLine();

        foreach (var airportGroup in byAirport)
        {
            sb.AppendLine($"  {TextFormatter.Colorize(airportGroup.Key, AnsiColor.Cyan)}:");

            var byType = airportGroup
                .GroupBy(p => p.Type)
                .OrderBy(g => TypeOrder(g.Key));

            foreach (var typeGroup in byType)
            {
                var label = typeGroup.Key switch
                {
                    CifpProcedureType.STAR => "STARs",
                    CifpProcedureType.SID => "SIDs",
                    CifpProcedureType.Approach => "Approaches",
                    _ => "?"
                };
                var ids = string.Join(", ", typeGroup
                    .Select(p => p.ProcedureId)
                    .OrderBy(i => i, StringComparer.OrdinalIgnoreCase));
                sb.AppendLine($"    {TextFormatter.Colorize(label + ":", AnsiColor.Gray)} {ids}");
            }
            sb.AppendLine();
        }

        sb.AppendLine($"  {TextFormatter.Colorize($"{procedures.Count} procedures across {byAirport.Count} airport{(byAirport.Count == 1 ? "" : "s")}", AnsiColor.Gray)}");

        return CommandResult.FromText(sb.ToString());
    }

    private static int TypeOrder(CifpProcedureType type) => type switch
    {
        CifpProcedureType.STAR => 0,
        CifpProcedureType.SID => 1,
        CifpProcedureType.Approach => 2,
        _ => 9
    };

    public IEnumerable<string> GetCompletions(string partial, int argIndex) => [];
}
