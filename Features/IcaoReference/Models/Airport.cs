namespace ZoaReference.Features.IcaoReference.Models;

public class Airport(string icaoId, string iataId, string localId, string name, string fir, double latitude, double longitude)
{
    public string IcaoId { get; set; } = icaoId;
    public string IataId { get; set; } = iataId;
    public string LocalId { get; set; } = localId;
    public string Name { get; set; } = name;
    public string Fir { get; set; } = fir;
    public double Latitude { get; set; } = latitude;
    public double Longitude { get; set; } = longitude;
}