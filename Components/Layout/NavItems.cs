namespace ZoaReference.Components.Layout;

public record NavItem(string Href, string Label);

public static class NavItems
{
    public static readonly Dictionary<string, NavItem> All = new()
    {
        ["routes"] = new("/routes", "Routes"),
        ["charts"] = new("/charts", "Charts"),
        ["codes"] = new("/codes", "ICAO Search"),
        ["procedures"] = new("/procedures", "Procedures"),
        ["atis"] = new("/atis", "D-ATIS"),
        ["videomaps"] = new("/videomaps", "STARS Video Maps"),
        ["scratchpads"] = new("/scratchpads", "Scratchpads"),
        ["positions"] = new("/positions", "Positions"),
        ["airspaceviz"] = new("/airspaceviz", "Airspace Viz"),
    };

    public static readonly List<string> DefaultOrder =
    [
        "routes", "charts", "codes", "procedures", "atis", 
        "videomaps", "scratchpads", "positions", "airspaceviz"
    ];
}
