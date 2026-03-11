namespace ZoaReference.Features.Nasr.Models;

public record AirwayRestriction(
    string AirwayId,
    string FromFix,
    string ToFix,
    int? Mea,
    int? Moca,
    string? Direction);
