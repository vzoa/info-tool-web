using ZoaReference.Features.Scratchpads.Models;

namespace ZoaReference.Features.Scratchpads.Repositories;


public class ScratchpadsRepository
{
    public IEnumerable<string> AllAirportIds => _dictionary.Keys;
    
    private readonly Dictionary<string, IReadOnlyList<Scratchpad>> _dictionary = new();

    public bool TryAddScratchpads(string key, IReadOnlyList<Scratchpad> scratchpads)
    {
        return _dictionary.TryAdd(key, scratchpads);
    }

    public bool TryGetValue(string key, out IReadOnlyList<Scratchpad>? scratchpads)
    {
        return _dictionary.TryGetValue(key, out scratchpads);
    }

    public void ClearAirports() => _dictionary.Clear();
}
