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
        ["videomaps"] = new("/videomaps", "Video Maps"),
        ["procedures"] = new("/procedures", "Procedures"),
        ["scratchpads"] = new("/scratchpads", "Scratchpads"),
        ["airspaceviz"] = new("/airspaceviz", "Airspace"),
        ["terminal"] = new("/terminal", "Terminal"),
    };

    public static readonly List<string> DefaultOrder =
    [
        "atis",
        "routes",
        "charts",
        "codes",
        "positions",
        "videomaps",
        "procedures",
        "scratchpads",
        "airspaceviz",
        "terminal"
    ];
}
