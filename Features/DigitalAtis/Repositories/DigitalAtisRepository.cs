using ZoaReference.Features.DigitalAtis.Models;

namespace ZoaReference.Features.DigitalAtis.Repositories;

public class DigitalAtisRepository(ILogger<DigitalAtisRepository> logger)
{
    private Dictionary<string, DigitalAtisRecord> _atisDict = new();

    public event EventHandler<NewAirportAddedArgs>? NewAirportAdded;
    public event EventHandler<NewInfoLetterArgs>? NewInfoLetter;

    public bool TryGetAtisForId(string id, out DigitalAtisRecord outAtis)
    {
        return _atisDict.TryGetValue(id, out outAtis);
    }

    public void UpdateAtisForId(string id, DigitalAtisRecord newAtis)
    {
        // Return early if just added an airport that didn't exist before
        if (!_atisDict.ContainsKey(id.ToUpper()))
        {
            _atisDict[id.ToUpper()] = newAtis;
            OnNewAirportAdded(new NewAirportAddedArgs() { Id = id.ToUpper() });
            return;
        }
        
        // Otherwise, check if new letter then update and raise event
        var existingRecord = _atisDict[id.ToUpper()];
        if (IsAnyNewLetter(existingRecord, newAtis))
        {
            logger.LogInformation("Here we found a new letter");
            _atisDict[id.ToUpper()] = newAtis;
            OnNewInfoLetter(new NewInfoLetterArgs() { Id = id.ToUpper() });
        }
    }
    
    public IEnumerable<Atis> GetAllAtis()
    {
        return _atisDict.Values.SelectMany(ConvertRecordToEnumerable);
    }

    private static IEnumerable<Atis> ConvertRecordToEnumerable(DigitalAtisRecord r)
    {
        var temp = new[] { r.Combined, r.Departure, r.Arrival };
        return temp.Where(a => a is not null)!;
    }

    private bool IsNewLetter(Atis? atis1, Atis? atis2)
    {
        // One of the 2 is null
        if (atis1 is null != atis2 is null)
        {
            return true;
        }
        
        // Both are null
        if (atis1 is null && atis2 is null)
        {
            return false;
        }
        
        // Neither are nulll
        if (atis1!.InfoLetter != atis2!.InfoLetter)
        {
            logger.LogInformation("{atis1} is not equal to {atis2}", atis1!.InfoLetter, atis2!.InfoLetter);
        }
        return atis1!.InfoLetter != atis2!.InfoLetter;
    }

    private bool IsAnyNewLetter(DigitalAtisRecord atis1, DigitalAtisRecord atis2)
    {
        return IsNewLetter(atis1.Combined, atis2.Combined)
               || IsNewLetter(atis1.Departure, atis2.Departure)
               || IsNewLetter(atis1.Arrival, atis2.Arrival);
    }

    private void OnNewAirportAdded(NewAirportAddedArgs e)
    {
        NewAirportAdded?.Invoke(this, e);
    }

    private void OnNewInfoLetter(NewInfoLetterArgs e)
    {
        NewInfoLetter?.Invoke(this, e);
    }
}

public record struct DigitalAtisRecord(string AirportId, Atis? Combined, Atis? Departure, Atis? Arrival);

public class NewAirportAddedArgs : EventArgs
{
    public string Id { get; init; } = "";
}

public class NewInfoLetterArgs : EventArgs
{
    public string Id { get; init; } = "";
}