using System.Text.RegularExpressions;
using Coravel.Invocable;
using Markdig.Syntax.Inlines;
using Microsoft.Extensions.Options;
using ZoaReference.Features.IcaoReference.Models;
using ZoaReference.Features.IcaoReference.Repositories;

namespace ZoaReference.Features.IcaoReference.ScheduledJobs;

public partial class FetchAndStoreAirports(ILogger<FetchAndStoreAirports> logger, IHttpClientFactory httpClientFactory, IOptionsMonitor<AppSettings> appSettings, AirportRepository airportRepository) : IInvocable
{
    public async Task Invoke()
    {
        using var httpClient = httpClientFactory.CreateClient();

        void parseAndAddAirport(string dataline)
        {
            try
            {
                // ICAO|Airport Name|Latitude Decimal|Longitude Decimal|IATA/LID|FIR|IsPseudo
                var fields = dataline.Split('|');
                var icao = fields[0].Trim();
                var name = fields[1].Trim();
                var latitude = double.Parse(fields[2].Trim());
                var longitude = double.Parse(fields[3].Trim());
                var iata = fields[4].Trim();
                var fir = fields[5].Trim();
                var isPseudo = int.Parse(fields[6].Trim().Substring(0, 1)) == 1;

                if (!isPseudo)
                {
                    // A few Australian airports have multiple entries, so we need to use TryAdd to not throw exception
                    airportRepository.TryAddAirport(icao,
                        new Airport(icao, iata, iata, name, fir, latitude, longitude));
                }
            }
            catch (SystemException e)
            {
                logger.LogWarning("Could not parse local airport: {ex}", e.ToString());
            }
        }

        var localAirportsUrl = appSettings.CurrentValue.Urls.LocalAirpotsDat;
        try
        {
            var localAirportsResponseStream = await httpClient.GetStringAsync(localAirportsUrl);
            using var localAirportsReader = new StringReader(localAirportsResponseStream);
            for (var line = localAirportsReader.ReadLine(); line is not null; line = localAirportsReader.ReadLine())
            {
                // Skip if line is empty or a commented line starting with ;
                if (line.Trim() == "" || line.StartsWith(';'))
                {
                    continue;
                }
                parseAndAddAirport(line);
            }
        }
        catch (HttpRequestException e)
        {
            logger.LogError("Error while fetching the local airports data: {ex}", e.ToString());
        }
        catch (Exception e)
        {
            logger.LogError("Unknown exception: {ex}", e.ToString());
        }

        var vatspyDataResponseBody = await httpClient.GetStringAsync(appSettings.CurrentValue.Urls.VatspyData);

        using (var reader = new StringReader(vatspyDataResponseBody))
        {
            var inAirportsSection = false;
            for (var line = reader.ReadLine(); line is not null; line = reader.ReadLine())
            {
                // Skip if line is empty or a commented line starting with ;
                if (line.Trim() == "" || line.StartsWith(';'))
                {
                    continue;
                }

                // Check if we're at a new section
                if (SectionHeaderRegex().IsMatch(line))
                {
                    // Section header found. Check if it's the Airports section
                    var header = SectionHeaderRegex().Match(line).Groups[1].Value;
                    inAirportsSection = header.Equals("AIRPORTS", StringComparison.OrdinalIgnoreCase);
                    continue; // Skip parsing current line because we know it's just a header, no data
                }

                // If we're in the Airports section, parse lines as airports
                if (inAirportsSection)
                {
                    parseAndAddAirport(line);
                }
            }
        }
    }

    [GeneratedRegex(@"\[(.*)\]")]
    private static partial Regex SectionHeaderRegex();
}