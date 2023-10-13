namespace ZoaReference.Features.Routes.Models;

public class AliasRouteRule
{
    public string DepartureAirport { get; set; }
    public int? DepartureRunway { get; set; }
    public string ArrivalAirport { get; set; }
    public int? ArrivalRunway { get; set; }
    public string Route { get; set; }
    public RouteAircraftTypes AllowedAircraftTypes { get; set; }

    public static RouteAircraftTypes StringToType(string typeStr)
    {
        return typeStr.ToUpper() switch
        {
            "J" => RouteAircraftTypes.Jet,
            "T" => RouteAircraftTypes.Turboprop,
            "P" => RouteAircraftTypes.Prop,
            _ => RouteAircraftTypes.Jet | RouteAircraftTypes.Turboprop | RouteAircraftTypes.Prop
        };
    }

    [Flags]
    public enum RouteAircraftTypes
    {
        Jet = 1 << 0,
        Turboprop = 1 << 1,
        Prop = 1 << 2
    }
}
