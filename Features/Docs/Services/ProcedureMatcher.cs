using System.Text.RegularExpressions;
using ZoaReference.Features.Docs.Models;

namespace ZoaReference.Features.Docs.Services;

public record ProcedureMatch(Document Document, double Score);

public static partial class ProcedureMatcher
{
    private static readonly Dictionary<string, string> AirportAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["SFO"] = "SAN FRANCISCO ATCT",
        ["OAK"] = "OAKLAND ATCT",
        ["SJC"] = "SAN JOSE ATCT",
        ["SMF"] = "SACRAMENTO ATCT",
        ["RNO"] = "RENO ATCT",
        ["FAT"] = "FRESNO ATCT TRACON SOP",
        ["MRY"] = "MONTEREY ATCT",
        ["NCT"] = "NORCAL TRACON",
        ["ZOA"] = "OAKLAND CENTER"
    };

    private static readonly Dictionary<string, string[]> ProcedureAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["NCT"] = ["NORCAL TRACON", "NORTHERN CALIFORNIA TRACON"],
        ["NORCAL"] = ["NCT", "NORTHERN CALIFORNIA TRACON"],
        ["ZOA"] = ["OAKLAND CENTER"]
    };

    private const double MinimumThreshold = 0.2;
    private const double AmbiguityThreshold = 0.15;

    public static (Document? BestMatch, List<ProcedureMatch> Matches) FindByName(
        IEnumerable<Document> documents,
        string procedureTerm)
    {
        var searchTerm = procedureTerm.ToUpperInvariant();

        var searchTerms = new List<string> { searchTerm };
        if (ProcedureAliases.TryGetValue(searchTerm, out var aliases))
        {
            searchTerms.AddRange(aliases);
        }

        foreach (var token in AlphanumTokenRegex().Matches(searchTerm).Select(m => m.Value))
        {
            if (token != searchTerm && ProcedureAliases.TryGetValue(token, out var tokenAliases))
            {
                foreach (var alias in tokenAliases)
                {
                    if (!searchTerms.Contains(alias, StringComparer.OrdinalIgnoreCase))
                    {
                        searchTerms.Add(alias);
                    }
                }
            }
        }

        var queryTokens = AlphanumTokenRegex().Matches(searchTerm)
            .Select(m => m.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var matches = new List<ProcedureMatch>();
        var seenUrls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var doc in documents)
        {
            if (seenUrls.Contains(doc.Url))
            {
                continue;
            }

            var bestScore = 0.0;
            foreach (var term in searchTerms)
            {
                var score = CalculateSimilarity(term, doc.Name);
                bestScore = Math.Max(bestScore, score);
            }

            if (bestScore > MinimumThreshold)
            {
                matches.Add(new ProcedureMatch(doc, bestScore));
                seenUrls.Add(doc.Url);
            }
        }

        if (matches.Count == 0)
        {
            return (null, []);
        }

        matches.Sort((a, b) => b.Score.CompareTo(a.Score));

        var bestMatch = matches[0];

        if (bestMatch.Score >= 1.0)
        {
            return (bestMatch.Document, matches);
        }

        if (queryTokens.Count > 1)
        {
            var fullMatches = matches.Where(m =>
            {
                var docTokens = AlphanumTokenRegex().Matches(m.Document.Name.ToUpperInvariant())
                    .Select(mt => mt.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
                return queryTokens.All(qt => docTokens.Contains(qt));
            }).ToList();

            if (fullMatches.Count == 1)
            {
                return (fullMatches[0].Document, matches);
            }

            if (fullMatches.Count > 1)
            {
                fullMatches.Sort((a, b) => b.Score.CompareTo(a.Score));
                if (fullMatches[0].Score - fullMatches[1].Score >= AmbiguityThreshold)
                {
                    return (fullMatches[0].Document, matches);
                }

                return (null, fullMatches);
            }
        }

        if (matches.Count > 1)
        {
            var secondScore = matches[1].Score;
            if (bestMatch.Score - secondScore < AmbiguityThreshold)
            {
                var closeMatches = matches
                    .Where(m => m.Score >= bestMatch.Score - AmbiguityThreshold)
                    .ToList();
                return (null, closeMatches);
            }
        }

        return (bestMatch.Document, matches);
    }

    private static double CalculateSimilarity(string query, string target)
    {
        query = ExpandAirportAliases(query);
        target = target.ToUpperInvariant();

        if (query == target)
        {
            return 1.0;
        }

        var queryTokens = AlphanumTokenRegex().Matches(query)
            .Select(m => m.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var targetTokens = AlphanumTokenRegex().Matches(target)
            .Select(m => m.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (queryTokens.Count == 0 || targetTokens.Count == 0)
        {
            return 0.0;
        }

        var intersection = queryTokens.Intersect(targetTokens, StringComparer.OrdinalIgnoreCase).Count();
        var union = queryTokens.Union(targetTokens, StringComparer.OrdinalIgnoreCase).Count();
        var jaccard = union > 0 ? (double)intersection / union : 0;

        var substringBonus = 0.0;
        if (target.Contains(query, StringComparison.OrdinalIgnoreCase))
        {
            substringBonus = 0.3;
        }
        else if (queryTokens.Any(qt => target.Contains(qt, StringComparison.OrdinalIgnoreCase)))
        {
            substringBonus = 0.15;
        }

        var prefixBonus = 0.0;
        var queryFirst = queryTokens.OrderBy(t => t, StringComparer.OrdinalIgnoreCase).FirstOrDefault() ?? "";
        if (queryFirst.Length > 0 && targetTokens.Any(tt =>
                tt.StartsWith(queryFirst, StringComparison.OrdinalIgnoreCase)))
        {
            prefixBonus = 0.1;
        }

        var editBonus = 0.0;
        if (intersection == 0)
        {
            foreach (var qt in queryTokens.Where(t => t.Length >= 4))
            {
                foreach (var tt in targetTokens.Where(t => t.Length >= 4))
                {
                    var dist = LevenshteinDistance(qt, tt);
                    var maxLen = Math.Max(qt.Length, tt.Length);
                    if (dist <= 2)
                    {
                        var similarity = 1.0 - ((double)dist / maxLen);
                        editBonus = Math.Max(editBonus, 0.4 * similarity);
                    }
                }
            }
        }

        return Math.Min(1.0, jaccard + substringBonus + prefixBonus + editBonus);
    }

    private static string ExpandAirportAliases(string query)
    {
        var queryUpper = query.ToUpperInvariant();
        var tokens = queryUpper.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        if (tokens.Length == 1 && AirportAliases.TryGetValue(tokens[0], out var alias))
        {
            return $"{tokens[0]} {alias}";
        }

        return queryUpper;
    }

    private static int LevenshteinDistance(string s1, string s2)
    {
        if (s1.Length < s2.Length)
        {
            (s1, s2) = (s2, s1);
        }

        if (s2.Length == 0)
        {
            return s1.Length;
        }

        var previousRow = new int[s2.Length + 1];
        for (var j = 0; j <= s2.Length; j++)
        {
            previousRow[j] = j;
        }

        for (var i = 0; i < s1.Length; i++)
        {
            var currentRow = new int[s2.Length + 1];
            currentRow[0] = i + 1;

            for (var j = 0; j < s2.Length; j++)
            {
                var insertions = previousRow[j + 1] + 1;
                var deletions = currentRow[j] + 1;
                var substitutions = previousRow[j] + (char.ToUpperInvariant(s1[i]) != char.ToUpperInvariant(s2[j]) ? 1 : 0);
                currentRow[j + 1] = Math.Min(insertions, Math.Min(deletions, substitutions));
            }

            previousRow = currentRow;
        }

        return previousRow[s2.Length];
    }

    [GeneratedRegex(@"[A-Z0-9]+", RegexOptions.IgnoreCase)]
    private static partial Regex AlphanumTokenRegex();
}
