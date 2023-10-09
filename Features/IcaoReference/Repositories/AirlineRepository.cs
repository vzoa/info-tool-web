using ZoaReference.Features.IcaoReference.Models;

namespace ZoaReference.Features.IcaoReference.Repositories;

public class AirlineRepository
{
    private readonly List<Airline> _repository = new();

    public void AddAirline(Airline airline) => _repository.Add(airline);
    
    public void AddAirlines(IEnumerable<Airline> airlines) => _repository.AddRange(airlines);

    public IEnumerable<Airline> GetAllRules() => _repository;

    public void ClearAirlines() => _repository.Clear();
}