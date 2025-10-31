namespace ZoaReference.Features.VnasData.Models;

public class PositionExtended(Position basePosition)
{
    public string Name => basePosition.Name ?? "";
    public string Callsign => basePosition.Callsign ?? "";
    public string RadioName => basePosition.RadioName ?? "";
    public int Frequency => basePosition.Frequency;

    public string Tcp => basePosition.StarsConfiguration is not null
        ? $"{basePosition.StarsConfiguration!.Subset}{basePosition.StarsConfiguration!.SectorId}"
        : "";
}