using ZoaReference.Features.IcaoReference.Models;

namespace ZoaReference.Features.IcaoReference.Repositories;

public class AirportRepository
{
    private readonly Dictionary<string, Airport> _dictionary = new();

    public bool TryAddAirport(string key, Airport airport)
    {
        return _dictionary.TryAdd(key, airport);
    }

    public bool TryGetValue(string key, out Airport? airport)
    {
        return _dictionary.TryGetValue(key, out airport);
    }

    public void ClearAirports() => _dictionary.Clear();
}