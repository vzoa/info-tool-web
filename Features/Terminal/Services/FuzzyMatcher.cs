using System.Text.RegularExpressions;

namespace ZoaReference.Features.Terminal.Services;

/// <summary>
/// Fuzzy matching utilities ported from the standalone CLI's <c>fuzzy.py</c>.
/// Used by the chart command and available to any command that needs
/// score-based matching with ambiguity detection.
/// </summary>
public static partial class FuzzyMatcher
{
    public sealed record ScoredMatch<T>(T Item, string Text, double Score);

    /// <summary>
    /// Standard Levenshtein (edit) distance with two-row dynamic programming.
    /// </summary>
    public static int Levenshtein(string s1, string s2)
    {
        if (s1.Length < s2.Length)
        {
            (s1, s2) = (s2, s1);
        }
        if (s2.Length == 0)
        {
            return s1.Length;
        }

        var previous = new int[s2.Length + 1];
        for (var j = 0; j <= s2.Length; j++)
        {
            previous[j] = j;
        }

        var current = new int[s2.Length + 1];
        for (var i = 0; i < s1.Length; i++)
        {
            current[0] = i + 1;
            for (var j = 0; j < s2.Length; j++)
            {
                var insertions = previous[j + 1] + 1;
                var deletions = current[j] + 1;
                var substitutions = previous[j] + (s1[i] == s2[j] ? 0 : 1);
                current[j + 1] = Math.Min(Math.Min(insertions, deletions), substitutions);
            }
            (previous, current) = (current, previous);
        }
        return previous[s2.Length];
    }

    /// <summary>
    /// Scores a query against a target string in the range [0, 1].
    /// Combines Jaccard token overlap, substring matching, prefix matching,
    /// and Levenshtein distance with typo tolerance. Direct port of
    /// <c>fuzzy.py:calculate_similarity</c> with one deliberate divergence:
    /// the minimum token length for edit-distance matching is 3 (not Python's 4),
    /// so short airport tokens like RNO can still edit-match RENO.
    /// </summary>
    public static double CalculateSimilarity(string query, string target)
    {
        var q = RunwayFormat.PadSingleDigit(query.ToUpperInvariant());
        var t = RunwayFormat.PadSingleDigit(target.ToUpperInvariant());

        if (q == t)
        {
            return 1.0;
        }

        var queryTokens = new HashSet<string>(
            TokenizeRegex().Matches(q).Select(m => m.Value));
        var targetTokens = new HashSet<string>(
            TokenizeRegex().Matches(t).Select(m => m.Value));

        if (queryTokens.Count == 0 || targetTokens.Count == 0)
        {
            return 0.0;
        }

        // Jaccard token overlap
        var intersectionCount = queryTokens.Intersect(targetTokens).Count();
        var unionCount = queryTokens.Union(targetTokens).Count();
        var jaccard = unionCount > 0 ? (double)intersectionCount / unionCount : 0.0;

        // Substring bonus
        double substringBonus = 0;
        if (t.Contains(q, StringComparison.Ordinal))
        {
            substringBonus = 0.3;
        }
        else if (queryTokens.Any(qt => t.Contains(qt, StringComparison.Ordinal)))
        {
            substringBonus = 0.15;
        }

        // Prefix bonus — query token is a prefix of a target token, scaled by coverage
        double prefixBonus = 0;
        foreach (var qt in queryTokens)
        {
            if (qt.Length < 2)
            {
                continue;
            }
            foreach (var tt in targetTokens)
            {
                if (tt.Length > qt.Length &&
                    tt.StartsWith(qt, StringComparison.Ordinal))
                {
                    var coverage = (double)qt.Length / tt.Length;
                    var bonus = 0.3 * coverage;
                    if (bonus > prefixBonus)
                    {
                        prefixBonus = bonus;
                    }
                }
            }
        }

        // Edit-distance bonus — per-token typo tolerance.
        //
        // Two deliberate divergences from fuzzy.py:calculate_similarity:
        //   1. Minimum token length lowered from 4 to 3 so airport tokens
        //      (RNO, OAK, SAC) can edit-match their full names (RENO,
        //      OAKLAND, SACRAMENTO).
        //   2. Gate is per-token, not per-query. Python skips edit bonus
        //      entirely if ANY Jaccard intersection exists; we instead
        //      skip only the tokens that already exact-matched, so the
        //      remaining query tokens can still earn edit bonus. Without
        //      this change, "RNO ONE" vs "RENO ONE" never benefits from
        //      RNO↔RENO similarity because ONE already matched.
        double editBonus = 0;
        foreach (var qt in queryTokens)
        {
            if (qt.Length < 3 || targetTokens.Contains(qt))
            {
                continue;
            }
            foreach (var tt in targetTokens)
            {
                if (tt.Length < 3 || queryTokens.Contains(tt))
                {
                    continue;
                }
                var dist = Levenshtein(qt, tt);
                if (dist <= 2)
                {
                    var maxLen = Math.Max(qt.Length, tt.Length);
                    var similarity = 1.0 - (double)dist / maxLen;
                    var bonus = 0.4 * similarity;
                    if (bonus > editBonus)
                    {
                        editBonus = bonus;
                    }
                }
            }
        }

        return Math.Min(1.0, jaccard + substringBonus + prefixBonus + editBonus);
    }

    /// <summary>
    /// Scores every candidate, optionally adds a per-item bonus, filters by
    /// <paramref name="minScore"/>, and returns matches sorted by score descending.
    /// </summary>
    public static IReadOnlyList<ScoredMatch<T>> ScoreAll<T>(
        string query,
        IEnumerable<T> items,
        Func<T, string> textSelector,
        Func<T, double>? scoreBonus = null,
        double minScore = 0.2)
    {
        var matches = new List<ScoredMatch<T>>();
        foreach (var item in items)
        {
            var text = textSelector(item);
            var score = CalculateSimilarity(query, text);
            if (scoreBonus is not null)
            {
                score += scoreBonus(item);
            }
            if (score >= minScore)
            {
                matches.Add(new ScoredMatch<T>(item, text, Math.Min(1.0, score)));
            }
        }
        matches.Sort((a, b) => b.Score.CompareTo(a.Score));
        return matches;
    }

    /// <summary>
    /// Applies ambiguity-detection heuristics to a sorted score list.
    /// Returns <c>(best, closeMatches)</c> where exactly one is populated:
    /// if the top match is unambiguous, <c>best</c> is set; otherwise
    /// <c>closeMatches</c> lists every candidate within
    /// <paramref name="ambiguityThreshold"/> of the top score for a picker.
    /// Mirrors <c>fuzzy.py:fuzzy_match</c>'s disambiguation logic.
    /// </summary>
    public static (ScoredMatch<T>? Best, IReadOnlyList<ScoredMatch<T>> CloseMatches)
        Disambiguate<T>(
            string query,
            IReadOnlyList<ScoredMatch<T>> scored,
            double ambiguityThreshold = 0.15)
    {
        if (scored.Count == 0)
        {
            return (null, Array.Empty<ScoredMatch<T>>());
        }
        if (scored.Count == 1)
        {
            return (scored[0], scored);
        }

        var best = scored[0];

        // Exact match always wins
        if (best.Score >= 1.0 - 0.0001)
        {
            return (best, scored);
        }

        // If the query has multiple tokens and exactly one candidate contains
        // all of them, auto-select that one — prevents short-token noise from
        // blocking an otherwise-clear match.
        var queryTokens = new HashSet<string>(
            TokenizeRegex().Matches(query.ToUpperInvariant()).Select(m => m.Value));
        if (queryTokens.Count > 1)
        {
            var fullMatches = scored
                .Where(s =>
                {
                    var targetTokens = new HashSet<string>(
                        TokenizeRegex().Matches(s.Text.ToUpperInvariant())
                            .Select(m => m.Value));
                    return queryTokens.IsSubsetOf(targetTokens);
                })
                .ToList();

            if (fullMatches.Count == 1)
            {
                return (fullMatches[0], scored);
            }
            if (fullMatches.Count > 1)
            {
                // Check for an exact-string match among the full-token matches
                var exact = fullMatches.FirstOrDefault(m => m.Score >= 1.0 - 0.0001);
                if (exact is not null)
                {
                    return (exact, scored);
                }
                // Still ambiguous, but narrow the picker to the full-token subset
                return (null, fullMatches);
            }
        }

        // Score-diff ambiguity: if top two are within threshold, picker
        var second = scored[1];
        if (best.Score - second.Score < ambiguityThreshold)
        {
            var cutoff = best.Score - ambiguityThreshold;
            var closeMatches = scored.Where(s => s.Score >= cutoff).ToList();
            return (null, closeMatches);
        }

        return (best, scored);
    }

    // Matches Python's re.findall(r"[A-Z0-9]+", ...) — digits and letters
    // stay in the same token, so "04L" tokenizes to a single "04L" rather
    // than splitting into "04" + "L". Callers that need finer granularity
    // can tokenize themselves.
    [GeneratedRegex(@"[A-Z0-9]+")]
    public static partial Regex TokenizeRegex();
}
