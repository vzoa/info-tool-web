namespace ZoaReference.Features.PirepEncoder.Models;

public sealed record DraftEntry
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N");

    public DateTimeOffset SavedAt { get; init; } = DateTimeOffset.UtcNow;

    public string Label { get; init; } = "";

    public Pirep Pirep { get; init; } = new();
}
