namespace ZoaReference.Features.Nasr.Services;

public static class AiracCycleHelper
{
    private static readonly DateOnly AiracEpoch = new(2025, 1, 23);
    private const int CycleDays = 28;

    public static (string CycleId, DateOnly EffectiveDate, DateOnly ExpirationDate) GetCurrentCycle()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var daysSinceEpoch = today.DayNumber - AiracEpoch.DayNumber;
        var cycleNumber = daysSinceEpoch / CycleDays;
        var effectiveDate = AiracEpoch.AddDays(cycleNumber * CycleDays);
        var expirationDate = effectiveDate.AddDays(CycleDays);
        var cycleId = effectiveDate.ToString("yyMMdd");
        return (cycleId, effectiveDate, expirationDate);
    }

    public static string GetCifpUrl()
    {
        var (_, effectiveDate, _) = GetCurrentCycle();
        return $"https://aeronav.faa.gov/Upload_313-d/cifp/CIFP_{effectiveDate:yyMMdd}.zip";
    }

    public static string GetNasrUrl()
    {
        var (_, effectiveDate, _) = GetCurrentCycle();
        return $"https://nfdc.faa.gov/webContent/28DaySub/{effectiveDate:yyyy-MM-dd}/";
    }

    public static TimeSpan TimeUntilNextCycle()
    {
        var (_, _, expirationDate) = GetCurrentCycle();
        var expiration = expirationDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var remaining = expiration - DateTime.UtcNow;
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }
}
