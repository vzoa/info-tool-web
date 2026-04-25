namespace ZoaReference.Features.PirepEncoder.Models;

public sealed record CloudLayer
{
    public string Base { get; init; } = "UNKN";

    public SkyCover Cover { get; init; } = SkyCover.SKC;

    public string? Tops { get; init; }
}
