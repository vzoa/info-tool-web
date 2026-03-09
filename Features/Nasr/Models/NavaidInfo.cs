namespace ZoaReference.Features.Nasr.Models;

public record NavaidInfo(
    string Id,
    string Name,
    string Type,
    string Frequency,
    double Latitude,
    double Longitude,
    string Variation);
