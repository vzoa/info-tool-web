using System.Text.RegularExpressions;
using AngleSharp.Html.Parser;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using ZoaReference.Common;
using ZoaReference.Features.Routes.Models;

namespace ZoaReference.Features.Routes.Services;

public partial class FlightAwareRouteService(IHttpClientFactory httpClientFactory, IOptionsMonitor<AppSettings> appSettings, IMemoryCache cache, ILogger<FlightAwareRouteService> logger)
{
    public async Task<AirportPairRouteSummary> FetchRoutesAsync(string departureIcao, string arrivalIcao)
    {
        // First check cache and return early if we have cached result
        if (cache.TryGetValue<AirportPairRouteSummary>(MakeCacheKey(departureIcao, arrivalIcao), out var cachedResult))
        {
            return cachedResult!;
        }

        // If not, fetch result from FlightAware
        var url = MakeUrl(departureIcao, arrivalIcao);
        try
        {
            // Setup return object
            var returnRoute = new AirportPairRouteSummary(departureIcao, arrivalIcao);

            // Open FlightAware IFR routing page
            var client = httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.UserAgent.TryParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:123.0) Gecko/20100101 Firefox/123.0");
            await using var stream = await client.GetStreamAsync(url);
            var parser = new HtmlParser();
            using var document = await parser.ParseDocumentAsync(stream);

            // Set up a temporary lookup dict for processing tables into objects
            var routesDict = new Dictionary<string, FlightRouteSummary>();

            // Initial parsing to select main tables and rows
            var tables = document.QuerySelectorAll("table.prettyTable.fullWidth");
            var summaryTable = tables[0];
            var flightsTable = tables[1];
            var summaryRows = summaryTable?.QuerySelectorAll("tr");
            var flightRows = flightsTable?.QuerySelectorAll("tr");

            // Iterate through Route Summary table and create new FlightRouteSummary for each row
            for (var i = 0; i < summaryRows.Length; i++)
            {
                // Ignore the first two rows which are table headers; every row after that is a data row
                if (i <= 1) { continue; }

                var tds = summaryRows[i].QuerySelectorAll("td");
                var newRouteSummary = new FlightRouteSummary
                {
                    RouteFrequency = int.Parse(tds[0].TextContent),
                    DepartureIcaoId = ParseId(tds[1].TextContent),
                    ArrivalIcaoId = ParseId(tds[2].TextContent),
                    MinAltitude = TryParseMinAltitude(tds[3].TextContent, out var minAlt) ? minAlt : null,
                    MaxAltitude = TryParseMaxAltitude(tds[3].TextContent, out var maxAlt) ? maxAlt : null,
                    Route = tds[4].TextContent,
                    DistanceMi = tds.Length > 5 ? (TryParseDistance(tds[5].TextContent, out var distance) ? distance : null) : null,
                    Flights = new List<RealWorldFlight>()
                };
                returnRoute.FlightRouteSummaries.Add(newRouteSummary);
                routesDict[newRouteSummary.Route] = newRouteSummary; // Temp lookup dict
            }

            // Iterate through itemized routes table and create new RealWorldFlight for each row.
            // Use the lookup dict to add to correct RouteSummary
            for (var i = 0; i < flightRows.Length; i++)
            {
                // Ignore the first two rows which are table headers; every row after that is a data row
                if (i <= 1) { continue; }

                var tds = flightRows[i].QuerySelectorAll("td");
                var newFlight = new RealWorldFlight
                {
                    Callsign = tds[1].TextContent.Trim(),
                    DepartureIcaoId = ParseId(tds[2].TextContent),
                    ArrivalIcaoId = ParseId(tds[3].TextContent),
                    AircraftIcaoId = tds[4].TextContent,
                    Altitude = Helpers.TryParseAltitude(tds[5].TextContent, out var alt) ? alt : null,
                    Route = tds[6].TextContent,
                    Distance = tds.Length > 7 ? (TryParseDistance(tds[7].TextContent, out var distance) ? distance : null) : null
                };

                // Add to associated RouteSummary
                if (routesDict.TryGetValue(newFlight.Route, out var route))
                {
                    route.Flights.Add(newFlight);
                }

                if (returnRoute.MostRecent.Count < 10)
                {
                    returnRoute.MostRecent.Add(newFlight);
                }
            }

            // Cache result before returning
            var expiration = DateTimeOffset.UtcNow.AddSeconds(appSettings.CurrentValue.CacheTtls.FlightAwareRoutes);
            cache.Set(MakeCacheKey(departureIcao, arrivalIcao), returnRoute, expiration);

            return returnRoute;
        }
        catch (HttpRequestException e)
        {
            logger.LogError("Error fetching FlightAware url {url}: {error}", url, e);
            throw e;
        }
    }

    public string MakeUrl(string departureIcao, string arrivalIcao)
    {
        return appSettings.CurrentValue.Urls.FlightAwareIfrRouteBase + "origin=" + departureIcao + "&destination=" + arrivalIcao;
    }

    private static bool TryParseMinAltitude(string altitudeRange, out int minAltitude)
    {
        var match = AltitudeRangeRegex().Match(altitudeRange);
        var parseString = match.Success ? match.Groups[1].Value : altitudeRange;
        return Helpers.TryParseAltitude(parseString, out minAltitude);
    }

    private static bool TryParseMaxAltitude(string altitudeRange, out int maxAltitude)
    {
        var match = AltitudeRangeRegex().Match(altitudeRange);
        var parseString = match.Success ? match.Groups[2].Value : altitudeRange;
        return Helpers.TryParseAltitude(parseString, out maxAltitude);
    }

    private static bool TryParseDistance(string distanceStr, out int distance)
    {
        var match = DistanceRegex().Match(distanceStr);
        var parseString = match.Success ? match.Groups[1].Value.Replace(",", string.Empty) : distanceStr;
        return int.TryParse(parseString, out distance);
    }

    private static (string, string) MakeCacheKey(string departureIcao, string arrivalIcao)
    {
        return ($"FlightAwareDeparture:{departureIcao.ToUpper()}", $"FlightAwareArrival:{arrivalIcao.ToUpper()}");
    }

    private static string ParseId(string id) => id.Length > 4 ? id[^4..] : id;

    [GeneratedRegex(@"([\s\S]+) - ([\s\S]+)")]
    private static partial Regex AltitudeRangeRegex();

    [GeneratedRegex("([0-9,]+) sm")]
    private static partial Regex DistanceRegex();
}