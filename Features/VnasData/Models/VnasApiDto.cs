using System.Text.Json.Serialization;

namespace ZoaReference.Features.VnasData.Models;

// Root myDeserializedClass = JsonSerializer.Deserialize<Root>(myJsonResponse);
public class Area
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("visibilityCenter")]
    public VisibilityCenter VisibilityCenter { get; set; }

    [JsonPropertyName("surveillanceRange")]
    public int SurveillanceRange { get; set; }

    [JsonPropertyName("ssaAirports")]
    public List<string> SsaAirports { get; set; }

    [JsonPropertyName("towerListConfigurations")]
    public List<TowerListConfiguration> TowerListConfigurations { get; set; }

    [JsonPropertyName("ldbBeaconCodesInhibited")]
    public bool LdbBeaconCodesInhibited { get; set; }

    [JsonPropertyName("pdbGroundSpeedInhibited")]
    public bool PdbGroundSpeedInhibited { get; set; }

    [JsonPropertyName("displayRequestedAltInFdb")]
    public bool DisplayRequestedAltInFdb { get; set; }

    [JsonPropertyName("useVfrPositionSymbol")]
    public bool UseVfrPositionSymbol { get; set; }

    [JsonPropertyName("showDestinationDepartures")]
    public bool ShowDestinationDepartures { get; set; }

    [JsonPropertyName("showDestinationSatelliteArrivals")]
    public bool ShowDestinationSatelliteArrivals { get; set; }

    [JsonPropertyName("showDestinationPrimaryArrivals")]
    public bool ShowDestinationPrimaryArrivals { get; set; }
}

public class AsdexConfiguration
{
    [JsonPropertyName("videoMapId")]
    public string VideoMapId { get; set; }

    [JsonPropertyName("defaultRotation")]
    public int DefaultRotation { get; set; }

    [JsonPropertyName("defaultZoomRange")]
    public int DefaultZoomRange { get; set; }

    [JsonPropertyName("targetVisibilityRange")]
    public int TargetVisibilityRange { get; set; }

    [JsonPropertyName("targetVisibilityCeiling")]
    public int TargetVisibilityCeiling { get; set; }

    [JsonPropertyName("fixRules")]
    public List<FixRule> FixRules { get; set; }

    [JsonPropertyName("useDestinationIdAsFix")]
    public bool UseDestinationIdAsFix { get; set; }

    [JsonPropertyName("runwayConfigurations")]
    public List<RunwayConfiguration> RunwayConfigurations { get; set; }

    [JsonPropertyName("positions")]
    public List<Position> Positions { get; set; }

    [JsonPropertyName("defaultPositionId")]
    public string DefaultPositionId { get; set; }

    [JsonPropertyName("towerLocation")]
    public TowerLocation TowerLocation { get; set; }
}

public class BeaconCodeBank
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("type")]
    public BankType Type { get; set; }

    [JsonPropertyName("start")]
    public int Start { get; set; }

    [JsonPropertyName("end")]
    public int End { get; set; }

    [JsonPropertyName("category")]
    public BankCategory Category { get; set; }

    [JsonPropertyName("priority")]
    public BankPriority Priority { get; set; }

    [JsonPropertyName("subset")]
    public int Subset { get; set; }

    public enum BankType
    {
        Vfr,
        Ifr,
        Any
    }

    public enum BankCategory
    {
        Internal,
        External
    }

    public enum BankPriority
    {
        Primary,
        Secondary,
        Tertiary
    }
}

public class Climbout
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; }
}

public class Climbvia
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; }
}

public class ContactInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; }
}

public class DepFreq
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; }
}

public class EramConfiguration
{
    [JsonPropertyName("nasId")]
    public string NasId { get; set; }

    [JsonPropertyName("geoMaps")]
    public List<GeoMap> GeoMaps { get; set; }

    [JsonPropertyName("emergencyChecklist")]
    public List<string> EmergencyChecklist { get; set; }

    [JsonPropertyName("positionReliefChecklist")]
    public List<string> PositionReliefChecklist { get; set; }

    [JsonPropertyName("internalAirports")]
    public List<object> InternalAirports { get; set; }

    [JsonPropertyName("beaconCodeBanks")]
    public List<BeaconCodeBank> BeaconCodeBanks { get; set; }

    [JsonPropertyName("starsHandoffIds")]
    public List<StarsHandoffId> StarsHandoffIds { get; set; }

    [JsonPropertyName("referenceFixes")]
    public List<string> ReferenceFixes { get; set; }

    [JsonPropertyName("asrSites")]
    public List<object> AsrSites { get; set; }

    [JsonPropertyName("conflictAlertFloor")]
    public int ConflictAlertFloor { get; set; }

    [JsonPropertyName("sectorId")]
    public string SectorId { get; set; }
}

public class Expect
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; }
}

public class ExternalBay
{
    [JsonPropertyName("facilityId")]
    public string FacilityId { get; set; }

    [JsonPropertyName("bayId")]
    public string BayId { get; set; }
}

public class Facility
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("type")]
    public FacilityType Type { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("childFacilities")]
    public List<Facility> ChildFacilities { get; set; }

    [JsonPropertyName("eramConfiguration")]
    public EramConfiguration EramConfiguration { get; set; }

    [JsonPropertyName("positions")]
    public List<Position> Positions { get; set; }

    [JsonPropertyName("neighboringFacilityIds")]
    public List<string> NeighboringFacilityIds { get; set; }

    [JsonPropertyName("nonNasFacilityIds")]
    public List<string> NonNasFacilityIds { get; set; }

    [JsonPropertyName("starsConfiguration")]
    public StarsConfiguration? StarsConfiguration { get; set; }

    [JsonPropertyName("towerCabConfiguration")]
    public TowerCabConfiguration? TowerCabConfiguration { get; set; }

    [JsonPropertyName("flightStripsConfiguration")]
    public FlightStripsConfiguration? FlightStripsConfiguration { get; set; }

    [JsonPropertyName("asdexConfiguration")]
    public AsdexConfiguration? AsdexConfiguration { get; set; }

    [JsonPropertyName("tdlsConfiguration")]
    public TdlsConfiguration? TdlsConfiguration { get; set; }

    public enum FacilityType
    {
        Artcc,
        Tracon,
        AtctTracon,
        AtctRapcon,
        Atct
    }
}

public class FilterMenu
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("labelLine1")]
    public string LabelLine1 { get; set; }

    [JsonPropertyName("labelLine2")]
    public string LabelLine2 { get; set; }
}

public class FixRule
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("searchPattern")]
    public string SearchPattern { get; set; }

    [JsonPropertyName("fixId")]
    public string FixId { get; set; }
}

public class FlightStripsConfiguration
{
    [JsonPropertyName("stripBays")]
    public List<StripBay> StripBays { get; set; }

    [JsonPropertyName("externalBays")]
    public List<ExternalBay> ExternalBays { get; set; }

    [JsonPropertyName("displayDestinationAirportIds")]
    public bool DisplayDestinationAirportIds { get; set; }

    [JsonPropertyName("displayBarcodes")]
    public bool DisplayBarcodes { get; set; }

    [JsonPropertyName("enableArrivalStrips")]
    public bool EnableArrivalStrips { get; set; }

    [JsonPropertyName("enableSeparateArrDepPrinters")]
    public bool EnableSeparateArrDepPrinters { get; set; }
}

public class GeoMap
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("labelLine1")]
    public string LabelLine1 { get; set; }

    [JsonPropertyName("labelLine2")]
    public string LabelLine2 { get; set; }

    [JsonPropertyName("filterMenu")]
    public List<FilterMenu> FilterMenu { get; set; }

    [JsonPropertyName("bcgMenu")]
    public List<string> BcgMenu { get; set; }

    [JsonPropertyName("videoMapIds")]
    public List<string> VideoMapIds { get; set; }
}

public class ImageReferencePoint
{
    [JsonPropertyName("lat")]
    public double Lat { get; set; }

    [JsonPropertyName("lon")]
    public double Lon { get; set; }
}

public class InitialAlt
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; }
}

public class LineDefaults
{
    [JsonPropertyName("style")]
    public string Style { get; set; }

    [JsonPropertyName("thickness")]
    public int Thickness { get; set; }

    [JsonPropertyName("bcg")]
    public int Bcg { get; set; }

    [JsonPropertyName("filters")]
    public List<object> Filters { get; set; }
}

public class LocalInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("value")]
    public string Value { get; set; }
}

public class MasterRunway
{
    [JsonPropertyName("runwayId")]
    public string RunwayId { get; set; }

    [JsonPropertyName("headingTolerance")]
    public int HeadingTolerance { get; set; }

    [JsonPropertyName("nearSideHalfWidth")]
    public double NearSideHalfWidth { get; set; }

    [JsonPropertyName("farSideHalfWidth")]
    public double FarSideHalfWidth { get; set; }

    [JsonPropertyName("nearSideDistance")]
    public double NearSideDistance { get; set; }

    [JsonPropertyName("regionLength")]
    public double RegionLength { get; set; }

    [JsonPropertyName("targetReferencePoint")]
    public TargetReferencePoint TargetReferencePoint { get; set; }

    [JsonPropertyName("targetReferenceLineHeading")]
    public double TargetReferenceLineHeading { get; set; }

    [JsonPropertyName("targetReferenceLineLength")]
    public int TargetReferenceLineLength { get; set; }

    [JsonPropertyName("targetReferencePointAltitude")]
    public int TargetReferencePointAltitude { get; set; }

    [JsonPropertyName("imageReferencePoint")]
    public ImageReferencePoint ImageReferencePoint { get; set; }

    [JsonPropertyName("imageReferenceLineHeading")]
    public double ImageReferenceLineHeading { get; set; }

    [JsonPropertyName("imageReferenceLineLength")]
    public int ImageReferenceLineLength { get; set; }

    [JsonPropertyName("tieModeOffset")]
    public double TieModeOffset { get; set; }

    [JsonPropertyName("descentPointDistance")]
    public double DescentPointDistance { get; set; }

    [JsonPropertyName("descentPointAltitude")]
    public int DescentPointAltitude { get; set; }

    [JsonPropertyName("abovePathTolerance")]
    public int AbovePathTolerance { get; set; }

    [JsonPropertyName("belowPathTolerance")]
    public int BelowPathTolerance { get; set; }

    [JsonPropertyName("defaultLeaderDirection")]
    public string DefaultLeaderDirection { get; set; }

    [JsonPropertyName("scratchpadPatterns")]
    public List<string> ScratchpadPatterns { get; set; }
}

public class Position
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("runwayIds")]
    public List<object> RunwayIds { get; set; }

    [JsonPropertyName("starred")]
    public bool Starred { get; set; }

    [JsonPropertyName("radioName")]
    public string RadioName { get; set; }

    [JsonPropertyName("callsign")]
    public string Callsign { get; set; }

    [JsonPropertyName("frequency")]
    public int Frequency { get; set; }

    [JsonPropertyName("starsConfiguration")]
    public StarsConfiguration StarsConfiguration { get; set; }

    [JsonPropertyName("eramConfiguration")]
    public EramConfiguration EramConfiguration { get; set; }
}

public class VnasApiRoot
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("lastUpdatedAt")]
    public DateTime LastUpdatedAt { get; set; }

    [JsonPropertyName("facility")]
    public Facility Facility { get; set; }

    [JsonPropertyName("visibilityCenters")]
    public List<VisibilityCenter> VisibilityCenters { get; set; }

    [JsonPropertyName("aliasesLastUpdatedAt")]
    public DateTime AliasesLastUpdatedAt { get; set; }

    [JsonPropertyName("videoMaps")]
    public List<VideoMap> VideoMaps { get; set; }
}

public class Rpc
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("index")]
    public int Index { get; set; }

    [JsonPropertyName("airportId")]
    public string AirportId { get; set; }

    [JsonPropertyName("positionSymbolTie")]
    public string PositionSymbolTie { get; set; }

    [JsonPropertyName("positionSymbolStagger")]
    public string PositionSymbolStagger { get; set; }

    [JsonPropertyName("masterRunway")]
    public MasterRunway MasterRunway { get; set; }

    [JsonPropertyName("slaveRunway")]
    public SlaveRunway SlaveRunway { get; set; }
}

public class RunwayConfiguration
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("arrivalRunwayIds")]
    public List<string> ArrivalRunwayIds { get; set; }

    [JsonPropertyName("departureRunwayIds")]
    public List<string> DepartureRunwayIds { get; set; }

    [JsonPropertyName("holdShortRunwayPairs")]
    public List<object> HoldShortRunwayPairs { get; set; }
}

public class ScratchpadRule
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("airportIds")]
    public List<string> AirportIds { get; set; }

    [JsonPropertyName("searchPattern")]
    public string SearchPattern { get; set; }

    [JsonPropertyName("minAltitude")]
    public int MinAltitude { get; set; }

    [JsonPropertyName("maxAltitude")]
    public int MaxAltitude { get; set; }

    [JsonPropertyName("template")]
    public string Template { get; set; }
}

public class Sid
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("transitions")]
    public List<Transition> Transitions { get; set; }
}

public class SlaveRunway
{
    [JsonPropertyName("runwayId")]
    public string RunwayId { get; set; }

    [JsonPropertyName("headingTolerance")]
    public int HeadingTolerance { get; set; }

    [JsonPropertyName("nearSideHalfWidth")]
    public double NearSideHalfWidth { get; set; }

    [JsonPropertyName("farSideHalfWidth")]
    public double FarSideHalfWidth { get; set; }

    [JsonPropertyName("nearSideDistance")]
    public double NearSideDistance { get; set; }

    [JsonPropertyName("regionLength")]
    public double RegionLength { get; set; }

    [JsonPropertyName("targetReferencePoint")]
    public TargetReferencePoint TargetReferencePoint { get; set; }

    [JsonPropertyName("targetReferenceLineHeading")]
    public double TargetReferenceLineHeading { get; set; }

    [JsonPropertyName("targetReferenceLineLength")]
    public int TargetReferenceLineLength { get; set; }

    [JsonPropertyName("targetReferencePointAltitude")]
    public int TargetReferencePointAltitude { get; set; }

    [JsonPropertyName("imageReferencePoint")]
    public ImageReferencePoint ImageReferencePoint { get; set; }

    [JsonPropertyName("imageReferenceLineHeading")]
    public double ImageReferenceLineHeading { get; set; }

    [JsonPropertyName("imageReferenceLineLength")]
    public int ImageReferenceLineLength { get; set; }

    [JsonPropertyName("tieModeOffset")]
    public double TieModeOffset { get; set; }

    [JsonPropertyName("descentPointDistance")]
    public double DescentPointDistance { get; set; }

    [JsonPropertyName("descentPointAltitude")]
    public int DescentPointAltitude { get; set; }

    [JsonPropertyName("abovePathTolerance")]
    public int AbovePathTolerance { get; set; }

    [JsonPropertyName("belowPathTolerance")]
    public int BelowPathTolerance { get; set; }

    [JsonPropertyName("defaultLeaderDirection")]
    public string DefaultLeaderDirection { get; set; }

    [JsonPropertyName("scratchpadPatterns")]
    public List<object> ScratchpadPatterns { get; set; }
}

public class StarsConfiguration
{
    [JsonPropertyName("subset")]
    public int Subset { get; set; }

    [JsonPropertyName("sectorId")]
    public string SectorId { get; set; }

    [JsonPropertyName("areaId")]
    public string AreaId { get; set; }

    [JsonPropertyName("colorSet")]
    public string ColorSet { get; set; }

    [JsonPropertyName("areas")]
    public List<Area> Areas { get; set; }

    [JsonPropertyName("internalAirports")]
    public List<string> InternalAirports { get; set; }

    [JsonPropertyName("beaconCodeBanks")]
    public List<BeaconCodeBank> BeaconCodeBanks { get; set; }

    [JsonPropertyName("rpcs")]
    public List<Rpc> Rpcs { get; set; }

    [JsonPropertyName("scratchpadRules")]
    public List<ScratchpadRule> ScratchpadRules { get; set; }

    [JsonPropertyName("rnavPatterns")]
    public List<object> RnavPatterns { get; set; }

    [JsonPropertyName("allow4CharacterScratchpad")]
    public bool Allow4CharacterScratchpad { get; set; }

    [JsonPropertyName("starsHandoffIds")]
    public List<StarsHandoffId> StarsHandoffIds { get; set; }
    
    [JsonPropertyName("videoMapIds")]
    public List<string> VideoMapIds { get; set; }

    [JsonPropertyName("artccHandoffsUseNasId")]
    public bool ArtccHandoffsUseNasId { get; set; }
}

public class StarsHandoffId
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("facilityId")]
    public string FacilityId { get; set; }

    [JsonPropertyName("handoffCharacter")]
    public string HandoffCharacter { get; set; }
}

public class StripBay
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("numberOfRacks")]
    public int NumberOfRacks { get; set; }
}

public class SymbolDefaults
{
    [JsonPropertyName("style")]
    public string Style { get; set; }

    [JsonPropertyName("size")]
    public int Size { get; set; }

    [JsonPropertyName("bcg")]
    public int Bcg { get; set; }

    [JsonPropertyName("filters")]
    public List<object> Filters { get; set; }
}

public class TargetReferencePoint
{
    [JsonPropertyName("lat")]
    public double Lat { get; set; }

    [JsonPropertyName("lon")]
    public double Lon { get; set; }
}

public class TdlsConfiguration
{
    [JsonPropertyName("mandatorySid")]
    public bool MandatorySid { get; set; }

    [JsonPropertyName("mandatoryClimbout")]
    public bool MandatoryClimbout { get; set; }

    [JsonPropertyName("mandatoryClimbvia")]
    public bool MandatoryClimbvia { get; set; }

    [JsonPropertyName("mandatoryInitialAlt")]
    public bool MandatoryInitialAlt { get; set; }

    [JsonPropertyName("mandatoryDepFreq")]
    public bool MandatoryDepFreq { get; set; }

    [JsonPropertyName("mandatoryExpect")]
    public bool MandatoryExpect { get; set; }

    [JsonPropertyName("mandatoryContactInfo")]
    public bool MandatoryContactInfo { get; set; }

    [JsonPropertyName("mandatoryLocalInfo")]
    public bool MandatoryLocalInfo { get; set; }

    [JsonPropertyName("sids")]
    public List<Sid> Sids { get; set; }

    [JsonPropertyName("climbouts")]
    public List<Climbout> Climbouts { get; set; }

    [JsonPropertyName("climbvias")]
    public List<Climbvia> Climbvias { get; set; }

    [JsonPropertyName("initialAlts")]
    public List<InitialAlt> InitialAlts { get; set; }

    [JsonPropertyName("depFreqs")]
    public List<DepFreq> DepFreqs { get; set; }

    [JsonPropertyName("expects")]
    public List<Expect> Expects { get; set; }

    [JsonPropertyName("contactInfos")]
    public List<ContactInfo> ContactInfos { get; set; }

    [JsonPropertyName("localInfos")]
    public List<LocalInfo> LocalInfos { get; set; }

    [JsonPropertyName("defaultSidId")]
    public string DefaultSidId { get; set; }

    [JsonPropertyName("defaultTransitionId")]
    public string DefaultTransitionId { get; set; }
}

public class TextDefaults
{
    [JsonPropertyName("size")]
    public int Size { get; set; }

    [JsonPropertyName("underline")]
    public bool Underline { get; set; }

    [JsonPropertyName("opaque")]
    public bool Opaque { get; set; }

    [JsonPropertyName("xOffset")]
    public int XOffset { get; set; }

    [JsonPropertyName("yOffset")]
    public int YOffset { get; set; }

    [JsonPropertyName("bcg")]
    public int Bcg { get; set; }

    [JsonPropertyName("filters")]
    public List<object> Filters { get; set; }
}

public class TowerCabConfiguration
{
    [JsonPropertyName("videoMapId")]
    public string VideoMapId { get; set; }

    [JsonPropertyName("defaultRotation")]
    public int DefaultRotation { get; set; }

    [JsonPropertyName("defaultZoomRange")]
    public int DefaultZoomRange { get; set; }

    [JsonPropertyName("aircraftVisibilityCeiling")]
    public int AircraftVisibilityCeiling { get; set; }
}

public class TowerListConfiguration
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("airportId")]
    public string AirportId { get; set; }

    [JsonPropertyName("range")]
    public int Range { get; set; }
}

public class TowerLocation
{
    [JsonPropertyName("lat")]
    public double Lat { get; set; }

    [JsonPropertyName("lon")]
    public double Lon { get; set; }
}

public class Transition
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("defaultClimbvia")]
    public string DefaultClimbvia { get; set; }

    [JsonPropertyName("defaultDepFreq")]
    public string DefaultDepFreq { get; set; }

    [JsonPropertyName("firstRoutePoint")]
    public string FirstRoutePoint { get; set; }

    [JsonPropertyName("defaultExpect")]
    public string DefaultExpect { get; set; }

    [JsonPropertyName("defaultInitialAlt")]
    public string DefaultInitialAlt { get; set; }
}

public class VideoMap
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; }

    [JsonPropertyName("shortName")]
    public string ShortName { get; set; }

    [JsonPropertyName("sourceFileName")]
    public string SourceFileName { get; set; }

    [JsonPropertyName("lastUpdatedAt")]
    public DateTime LastUpdatedAt { get; set; }

    [JsonPropertyName("starsBrightnessCategory")]
    public string StarsBrightnessCategory { get; set; }

    [JsonPropertyName("starsId")]
    public int StarsId { get; set; }

    [JsonPropertyName("starsAlwaysVisible")]
    public bool StarsAlwaysVisible { get; set; }

    [JsonPropertyName("tdmOnly")]
    public bool TdmOnly { get; set; }
}

public class VisibilityCenter
{
    [JsonPropertyName("lat")]
    public double Lat { get; set; }

    [JsonPropertyName("lon")]
    public double Lon { get; set; }
}
