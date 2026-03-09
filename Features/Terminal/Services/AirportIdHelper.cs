namespace ZoaReference.Features.Terminal.Services;

public static class AirportIdHelper
{
    public static string NormalizeToIcao(string input)
    {
        var upper = input.Trim().ToUpperInvariant();
        if (upper.Length == 3)
        {
            return $"K{upper}";
        }
        return upper;
    }

    public static string NormalizeToFaa(string input)
    {
        var upper = input.Trim().ToUpperInvariant();
        if (upper.Length == 4 && upper.StartsWith('K'))
        {
            return upper[1..];
        }
        return upper;
    }
}
