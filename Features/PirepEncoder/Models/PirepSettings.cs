namespace ZoaReference.Features.PirepEncoder.Models;

public sealed record PirepSettings
{
    public string? DefaultSaIdentifier { get; init; }

    public bool PrefixWithSaIdentifier { get; init; } = true;

    public string? DefaultAircraftType { get; init; }
}
