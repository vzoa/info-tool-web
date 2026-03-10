namespace ZoaReference.Features.Charts.Models;

public enum FixRole
{
    None,
    IAF,
    IF,
    FAF,
    Feeder
}

public record CifpStarData(
    string Identifier,
    List<string> Waypoints,
    List<string> Transitions
);

public record CifpApproachFix(
    string ApproachId,
    string Transition,
    string FixIdentifier,
    FixRole Role,
    int Sequence
);

public record CifpApproach(
    string Airport,
    string ApproachId,
    string Runway
)
{
    public List<CifpApproachFix> Fixes { get; init; } = [];

    public List<string> IafFixes =>
        Fixes.Where(f => f.Role == FixRole.IAF)
            .Select(f => f.FixIdentifier)
            .Distinct()
            .ToList();

    public List<string> IfFixes =>
        Fixes.Where(f => f.Role == FixRole.IF)
            .Select(f => f.FixIdentifier)
            .Distinct()
            .ToList();

    public Dictionary<string, string> FeederPaths
    {
        get
        {
            var feeders = new Dictionary<string, string>();
            var iafIfSet = new HashSet<string>(IafFixes.Concat(IfFixes));

            var transitions = Fixes
                .Where(f => !string.IsNullOrEmpty(f.Transition))
                .GroupBy(f => f.Transition);

            foreach (var group in transitions)
            {
                var sorted = group.OrderBy(f => f.Sequence).ToList();
                if (sorted.Count == 0)
                {
                    continue;
                }

                var firstFix = sorted[0].FixIdentifier;
                if (iafIfSet.Contains(firstFix))
                {
                    continue;
                }

                var destFix = sorted.FirstOrDefault(f => f.Role is FixRole.IAF or FixRole.IF);
                if (destFix is not null && !feeders.ContainsKey(firstFix))
                {
                    feeders[firstFix] = destFix.FixIdentifier;
                }
            }

            return feeders;
        }
    }

    public List<string> FeederFixes => [.. FeederPaths.Keys];

    public List<string> EntryFixes =>
        IafFixes.Union(IfFixes).Distinct().ToList();
}

public record ApproachConnection(
    string ApproachChartName,
    string ConnectingFix,
    FixRole FixType,
    string? Runway
);
