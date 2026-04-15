namespace ZoaReference.Features.Charts.Models;

public enum CifpProcedureType
{
    SID,
    STAR,
    Approach
}

public record CifpProcedureDetail(
    string Airport,
    string Name,
    CifpProcedureType ProcedureType,
    List<CifpLeg> Legs);

public record CifpLeg(
    string FixId,
    string PathTerminator,
    string? AltitudeConstraint,
    int? SpeedConstraint,
    double? Course,
    double? Distance,
    FixRole Role);

/// <summary>
/// One (airport, procedure, type) tuple returned by a cross-airport CIFP fix
/// search — e.g. when answering "which procedures contain MYJAW?".
/// </summary>
public record CifpFixUsage(
    string Airport,
    string ProcedureId,
    CifpProcedureType Type);
