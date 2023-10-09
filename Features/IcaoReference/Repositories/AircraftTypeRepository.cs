using ZoaReference.Features.IcaoReference.Models;

namespace ZoaReference.Features.IcaoReference.Repositories;

public class AircraftTypeRepository
{
    public IEnumerable<AircraftType> AllAircraftTypes => _repository;
    
    private readonly List<AircraftType> _repository = new();

    public void AddAircraftType(AircraftType type) => _repository.Add(type);
    
    public void AddAircraftTypes(IEnumerable<AircraftType> types) => _repository.AddRange(types);

    public void ClearAircraftTypes() => _repository.Clear();
}