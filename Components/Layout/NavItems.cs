namespace ZoaReference.Components.Layout;

public record NavItem(string Href, string Label);

public static class NavItems
{
    public static readonly Dictionary<string, NavItem> All = new()
    {
        ["atis"] = new("/atis", "ATIS"),
        ["routes"] = new("/routes", "Routes"),
        ["charts"] = new("/charts", "Charts"),
        ["codes"] = new("/codes", "ICAO Codes"),
        ["positions"] = new("/positions", "Positions"),
        ["videomaps"] = new("/videomaps", "STARS Video Maps"),
        ["procedures"] = new("/procedures", "Procedures"),
        ["scratchpads"] = new("/scratchpads", "Scratchpads"),
        ["airspaceviz"] = new("/airspaceviz", "Airspace Viz"),
    };

    public static readonly List<string> DefaultOrder =
    [
        "atis", "routes", "charts", "codes", "positions",
        "videomaps", "procedures", "scratchpads", "airspaceviz"
    ];
}
