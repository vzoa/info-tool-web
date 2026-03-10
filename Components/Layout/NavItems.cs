namespace ZoaReference.Components.Layout;

public record NavItem(string Href, string Label);

public static class NavItems
{
    public static readonly Dictionary<string, NavItem> All = new()
    {
        ["atis"] = new("/atis", "D-ATIS"),
        ["routes"] = new("/routes", "Routes"),
        ["charts"] = new("/charts", "Charts"),
        ["codes"] = new("/codes", "ICAO Search"),
        ["positions"] = new("/positions", "Positions"),
        ["videomaps"] = new("/videomaps", "STARS Video Maps"),
        ["procedures"] = new("/procedures", "Procedures"),
        ["scratchpads"] = new("/scratchpads", "Scratchpads"),
        ["airspaceviz"] = new("/airspaceviz", "Airspace"),
    };

    public static readonly List<string> DefaultOrder =
    [
        "routes",
        "charts",
        "codes",
        "procedures",
        "airspaceviz",
        "atis",
        "videomaps",
        "scratchpads",
        "positions"
    ];
}