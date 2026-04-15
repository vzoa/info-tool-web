using System.Text.RegularExpressions;

namespace ZoaReference.Features.Terminal.Services;

/// <summary>
/// Shared helpers for handling runway identifiers where the user may type
/// a single digit (<c>4L</c>, <c>4</c>) but the data stores the zero-padded
/// form (<c>04L</c>), or vice versa. Different commands need different
/// directions of the same fix, so the logic lives here.
/// </summary>
public static partial class RunwayFormat
{
    /// <summary>
    /// Pads single-digit runway numbers with a leading zero so queries like
    /// <c>"RNAV 4L"</c> match chart names like <c>"RNAV (GPS) RWY 04L"</c>.
    /// Leaves two-digit runways (<c>"28R"</c>) and non-runway digits
    /// untouched because the regex only fires at word boundaries around a
    /// lone digit followed by an optional L/R/C.
    /// </summary>
    public static string PadSingleDigit(string input) =>
        SingleDigitRunwayRegex().Replace(input, "0$1");

    /// <summary>
    /// True when a user-supplied runway filter matches a stored runway
    /// identifier after stripping leading zeros on both sides. Handles the
    /// common case where the user types <c>"4"</c> or <c>"4L"</c> and the
    /// data has <c>"04L"</c>. Matches the CLI's <c>_filter_by_runways</c>
    /// behavior: prefix match on the normalized form, so <c>"17"</c>
    /// catches <c>"17"</c>, <c>"17L"</c>, and <c>"17R"</c>.
    /// </summary>
    public static bool FilterMatches(string filter, string runway)
    {
        var normalizedFilter = filter.TrimStart('0');
        var normalizedRunway = runway.TrimStart('0');
        return normalizedRunway.Equals(normalizedFilter, StringComparison.OrdinalIgnoreCase)
            || normalizedRunway.StartsWith(normalizedFilter, StringComparison.OrdinalIgnoreCase);
    }

    [GeneratedRegex(@"\b(\d[LRC]?)\b", RegexOptions.IgnoreCase)]
    private static partial Regex SingleDigitRunwayRegex();
}
