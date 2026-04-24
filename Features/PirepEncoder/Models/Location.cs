using System.Collections.Generic;

namespace ZoaReference.Features.PirepEncoder.Models;

public sealed record LocationSegment
{
    public string Fix { get; init; } = "";

    public string? RadialDistance { get; init; }
}

public sealed record Location
{
    public IReadOnlyList<LocationSegment> Segments { get; init; } = new List<LocationSegment>();
}
