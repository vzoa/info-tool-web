namespace ZoaReference.Features.Routes.Models;

public class AirportPairRouteSummary(string departureIcaoId, string arrivalIcaoId)
{
    public string DepartureIcaoId { get; set; } = departureIcaoId;
    public string ArrivalIcaoId { get; set; } = arrivalIcaoId;
    public ICollection<FlightRouteSummary> FlightRouteSummaries { get; set; } = new List<FlightRouteSummary>();
    public ICollection<RealWorldFlight> MostRecent { get; set; } = new List<RealWorldFlight>();

    //[JsonIgnore]
    //public ICollection<RealWorldFlight> Flights => RouteSummaries.SelectMany(r => r.Flights).ToList();
}

public class FlightRouteSummary
{
    public string DepartureIcaoId { get; set; }
    public string ArrivalIcaoId { get; set; }
    public int RouteFrequency { get; set; }
    public int? MinAltitude { get; set; }
    public int? MaxAltitude { get; set; }
    public string Route { get; set; }
    public int? DistanceMi { get; set; }
    public ICollection<RealWorldFlight> Flights { get; set; }
}

public class RealWorldFlight
{
    public string DepartureIcaoId { get; set; }
    public string ArrivalIcaoId { get; set; }
    public string Callsign { get; set; }
    public string AircraftIcaoId { get; set; }
    public int? Altitude { get; set; }
    public string Route { get; set; }
    public int? Distance { get; set; }
}