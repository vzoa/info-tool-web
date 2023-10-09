namespace ZoaReference.Features.IcaoReference.Models;

public class AircraftType
{
    public string IcaoId { get; set; }
    public string Manufacturer { get; set; }
    public string Model { get; set; }
    public string Class { get; set; }
    public string EngineType { get; set; }
    public string EngineCount { get; set; }
    public string IcaoWakeTurbulenceCategory { get; set; }
    public string FaaEngineNumberType { get; set; }
    public string FaaWeightClass { get; set; }
    public string ConsolidatedWakeTurbulenceCategory { get; set; }
    public string SameRunwaySeparationCategory { get; set; }
    public string LandAndHoldShortGroup { get; set; }

    public AircraftType() { }

    public AircraftType(string icaoId, string manufacturer, string model, string aircraftClass, string engineType, string engineCount, string icaoWakeTurbulenceCategory, string faaEngineNumberType, string faaWeightClass, string consolidatedWakeTurbulenceCategory, string sameRunwaySeparationCategory, string landAndHoldShortGroup)
    {
        IcaoId = icaoId;
        Manufacturer = manufacturer;
        Model = model;
        Class = aircraftClass;
        EngineType = engineType;
        EngineCount = engineCount;
        IcaoWakeTurbulenceCategory = icaoWakeTurbulenceCategory;
        FaaEngineNumberType = faaEngineNumberType;
        FaaWeightClass = faaWeightClass;
        ConsolidatedWakeTurbulenceCategory = consolidatedWakeTurbulenceCategory;
        SameRunwaySeparationCategory = sameRunwaySeparationCategory;
        LandAndHoldShortGroup = landAndHoldShortGroup;
    }
}
