namespace ZoaReference.Features.Routes.Models;

public class AliasRouteRule
{
    public string DepartureAirport { get; set; }
    public int? DepartureRunway { get; set; }
    public string ArrivalAirport { get; set; }
    public int? ArrivalRunway { get; set; }
    public string Route { get; set; }
    public RouteAircraftType AllowedAircraftType { get; set; }

    public static RouteAircraftType StringToType(string typeStr)
    {
        return typeStr.ToUpper() switch
        {
            "J" => RouteAircraftType.Jet,
            "T" => RouteAircraftType.Turboprop,
            "P" => RouteAircraftType.Prop,
            _ => RouteAircraftType.Jet | RouteAircraftType.Turboprop | RouteAircraftType.Prop
        };
    }

    [Flags]
    public enum RouteAircraftType
    {
        Jet = 1 << 0,
        Turboprop = 1 << 1,
        Prop = 1 << 2
    }
}
