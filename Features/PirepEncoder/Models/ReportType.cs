namespace ZoaReference.Features.PirepEncoder.Models;

public enum ReportType
{
    Routine,
    Urgent,
}

public static class ReportTypeExtensions
{
    public static string ToCode(this ReportType type) => type switch
    {
        ReportType.Routine => "UA",
        ReportType.Urgent => "UUA",
        _ => "UA",
    };

    public static ReportType FromCode(string code) => code switch
    {
        "UUA" => ReportType.Urgent,
        _ => ReportType.Routine,
    };
}
