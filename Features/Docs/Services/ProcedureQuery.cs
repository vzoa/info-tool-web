using System.Text.RegularExpressions;

namespace ZoaReference.Features.Docs.Services;

/// <summary>
/// Parsed procedure query with optional section and search terms.
/// </summary>
public partial record ProcedureQuery(
    string ProcedureTerm,
    string? SectionTerm,
    string? SearchTerm)
{
    private static readonly HashSet<string> ProcKeywords =
        ["ATCT", "SOP", "TRACON", "LOA", "CPS", "CENTER"];

    public static ProcedureQuery Parse(string[] parts, ProcedureSearchConfig config)
    {
        if (parts.Length == 0)
        {
            return new ProcedureQuery("", null, null);
        }

        var procedureParts = new List<string>();
        string? sectionTerm = null;
        string? searchTerm = null;

        var i = 0;
        while (i < parts.Length)
        {
            var part = parts[i];
            var partUpper = part.ToUpperInvariant();

            var isSectionStart =
                SectionNumberRegex().IsMatch(partUpper) ||
                (i > 0
                 && !ProcKeywords.Contains(partUpper)
                 && !config.AirportCodes.Contains(partUpper)
                 && part.Length > 1);

            if (i == 1 && procedureParts.Count > 0)
            {
                var firstUpper = procedureParts[0].ToUpperInvariant();
                if ((config.AirportCodes.Contains(firstUpper) || ProcKeywords.Contains(firstUpper))
                    && isSectionStart)
                {
                    break;
                }

                if (config.AirportAliases.ContainsKey(firstUpper) && !ProcKeywords.Contains(partUpper))
                {
                    break;
                }

                if (!config.AirportCodes.Contains(firstUpper) && !ProcKeywords.Contains(firstUpper))
                {
                    break;
                }
            }

            if (procedureParts.Count >= 2)
            {
                var firstUpper = procedureParts[0].ToUpperInvariant();
                var lastUpper = procedureParts[^1].ToUpperInvariant();
                if (config.AirportCodes.Contains(firstUpper) && ProcKeywords.Contains(lastUpper))
                {
                    break;
                }
            }

            if (SectionNumberRegex().IsMatch(partUpper))
            {
                break;
            }

            procedureParts.Add(part);
            i++;
        }

        var remaining = parts[i..];
        if (remaining.Length >= 2)
        {
            sectionTerm = string.Join(" ", remaining[..^1]);
            searchTerm = remaining[^1];
        }
        else if (remaining.Length == 1)
        {
            sectionTerm = remaining[0];
        }

        var procedureTerm = string.Join(" ", procedureParts);

        if (config.ClassDAirports.Contains(procedureTerm.ToUpperInvariant()))
        {
            var airportCode = procedureTerm.ToUpperInvariant();
            return new ProcedureQuery(
                "Class D Airports",
                $"K{airportCode}",
                sectionTerm);
        }

        return new ProcedureQuery(procedureTerm, sectionTerm, searchTerm);
    }

    [GeneratedRegex(@"^\d+[-.]")]
    private static partial Regex SectionNumberRegex();
}
