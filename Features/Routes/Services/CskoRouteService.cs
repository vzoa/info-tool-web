using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using ZoaReference.Features.Routes.Models;

namespace ZoaReference.Features.Routes.Services;

public class CskoRouteService(
    IHttpClientFactory httpClientFactory,
    IOptionsMonitor<AppSettings> appSettings,
    IMemoryCache cache,
    ILogger<CskoRouteService> logger)
{
    public async Task<AirportPairRouteSummary> FetchRoutesAsync(string departureIcao, string arrivalIcao)
    {
        if (cache.TryGetValue<AirportPairRouteSummary>(MakeCacheKey(departureIcao, arrivalIcao), out var routeSummary))
        {
            return routeSummary!;
        }

        var url = MakeUrl(departureIcao, arrivalIcao);
        try
        {
            var client = httpClientFactory.CreateClient();
            var routes = await client.GetFromJsonAsync<FlightRoutesRoot>(url);
            var returnRouteSummary = new AirportPairRouteSummary(departureIcao, arrivalIcao);
            var routesDict = new Dictionary<string, FlightRouteSummary>();

            if (routes?.Routes is not null)
            {
                foreach (var fetchedSummary in routes.Routes)
                {
                    var newRoute = new FlightRouteSummary
                    {
                        RouteFrequency = fetchedSummary.Count,
                        DepartureIcaoId = fetchedSummary.Departure,
                        ArrivalIcaoId = fetchedSummary.Arrival,
                        MinAltitude = fetchedSummary.MinAltitude,
                        MaxAltitude = fetchedSummary.MaxAltitude,
                        Route = fetchedSummary.RouteText,
                        DistanceMi = null,
                        Flights = new List<RealWorldFlight>()
                    };
                    returnRouteSummary.FlightRouteSummaries.Add(newRoute);
                    routesDict[fetchedSummary.RouteText] = newRoute;
                }
            }

            if (routes?.MostRecent is not null)
            {
                foreach (var flight in routes.MostRecent)
                {
                    var newFlight = new RealWorldFlight
                    {
                        DepartureIcaoId = flight.Departure,
                        ArrivalIcaoId = flight.Arrival,
                        Callsign = flight.AircraftId,
                        AircraftIcaoId = flight.AircraftType,
                        Altitude = flight.AssignedAltitude,
                        Route = flight.RouteText,
                        Distance = null
                    };

                    if (routesDict.TryGetValue(flight.RouteText, out var route))
                    {
                        route.Flights.Add(newFlight);
                    }

                    returnRouteSummary.MostRecent.Add(newFlight);
                }
            }

            // Cache result before returning
            var expiration = DateTimeOffset.UtcNow.AddSeconds(appSettings.CurrentValue.CacheTtls.FlightAwareRoutes);
            cache.Set(MakeCacheKey(departureIcao, arrivalIcao), routes, expiration);

            return returnRouteSummary;
        }
        catch (HttpRequestException e)
        {
            logger.LogError("Error fetching Route Service url {url}: {error}", url, e);
            throw e;
        }
    }

    private string MakeUrl(string departureIcao, string arrivalIcao) => appSettings.CurrentValue.Urls.CskoRouteBase +
                                                                        "departure=" + departureIcao + "&arrival=" +
                                                                        arrivalIcao + "&since" +
                                                                        DaysAgoTimestamp(31);

    private static (string, string) MakeCacheKey(string departureIcao, string arrivalIcao) => (
        $"CskoDeparture:{departureIcao.ToUpper()}", $"CskoArrival:{arrivalIcao.ToUpper()}");

    private static string DaysAgoTimestamp(int days) =>
        DateTimeOffset.UtcNow.AddDays(-1 * days).ToUnixTimeMilliseconds().ToString();
}

public class FlightRoutesRoot
{
    [JsonPropertyName("routes")] public List<Route> Routes { get; set; }

    [JsonPropertyName("adapted_routes")] public List<AdaptedRoute> AdaptedRoutes { get; set; }

    [JsonPropertyName("most_recent")] public List<MostRecent> MostRecent { get; set; }
}

public class AdaptedRoute
{
    [JsonPropertyName("departure")] public string Departure { get; set; }

    [JsonPropertyName("arrival")] public string Arrival { get; set; }

    [JsonPropertyName("route_type")] public string RouteType { get; set; }

    [JsonPropertyName("route_id")] public string RouteId { get; set; }

    [JsonPropertyName("route")] public string Route { get; set; }
}

public class MostRecent
{
    [JsonPropertyName("aircraft_id")] public string AircraftId { get; set; }

    [JsonPropertyName("departure")] public string Departure { get; set; }

    [JsonPropertyName("arrival")] public string Arrival { get; set; }

    [JsonPropertyName("assigned_altitude")]
    public int? AssignedAltitude { get; set; }

    [JsonPropertyName("route_text")] public string RouteText { get; set; }

    [JsonPropertyName("aircraft_type")] public string AircraftType { get; set; }

    [JsonPropertyName("equipment_qualifier")]
    public string EquipmentQualifier { get; set; }

    [JsonPropertyName("timestamp")] public object Timestamp { get; set; }
}

public class Route
{
    [JsonPropertyName("count")] public int Count { get; set; }

    [JsonPropertyName("departure")] public string Departure { get; set; }

    [JsonPropertyName("arrival")] public string Arrival { get; set; }

    [JsonPropertyName("route_text")] public string RouteText { get; set; }

    [JsonPropertyName("min_altitude")] public int? MinAltitude { get; set; }

    [JsonPropertyName("max_altitude")] public int? MaxAltitude { get; set; }

    [JsonPropertyName("equipment_qualifiers")]
    public string EquipmentQualifiers { get; set; }

    [JsonPropertyName("aircraft_types")] public string AircraftTypes { get; set; }
}
