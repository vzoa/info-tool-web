using System.Text.RegularExpressions;
using Coravel.Invocable;
using Microsoft.Extensions.Options;
using ZoaReference.Features.IcaoReference.Models;
using ZoaReference.Features.IcaoReference.Repositories;

namespace ZoaReference.Features.IcaoReference.ScheduledJobs;

public partial class FetchAndStoreAirports(ILogger<FetchAndStoreAirports> logger, IHttpClientFactory httpClientFactory, IOptionsMonitor<AppSettings> appSettings, AirportRepository airportRepository) : IInvocable
{
    public async Task Invoke()
    {
        var httpClient = httpClientFactory.CreateClient();
        
        var returnDict = new Dictionary<string, Airport>();
        string responseBody = await httpClient.GetStringAsync(appSettings.CurrentValue.Urls.VatspyData);

        using (var reader = new StringReader(responseBody))
        {
            bool inAirportsSection = false;
            for (string? line = reader.ReadLine(); line is not null; line = reader.ReadLine())
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
                    string header = SectionHeaderRegex().Match(line).Groups[1].Value;
                    inAirportsSection = header.Equals("AIRPORTS", StringComparison.OrdinalIgnoreCase);
                    continue; // Skip parsing current line because we know it's just a header, no data
                }

                // If we're in the Airports section, parse lines as airports
                if (inAirportsSection)
                {
                    // ICAO|Airport Name|Latitude Decimal|Longitude Decimal|IATA/LID|FIR|IsPseudo
                    var fields = line.Split('|');
                    var icao = fields[0].Trim();
                    var name = fields[1].Trim();
                    var latitude = double.Parse(fields[2].Trim());
                    var longitude = double.Parse(fields[3].Trim());
                    var iata = fields[4].Trim();
                    var lid = iata;
                    var fir = fields[5].Trim();
                    var isPseudo = int.Parse(fields[6].Trim().Substring(0, 1)) == 1;

                    if (!isPseudo)
                    {
                        // A few Australian airports have multiple entries, so we need to use TryAdd to not throw exception
                        airportRepository.TryAddAirport(icao,
                            new Airport(icao, iata, lid, name, fir, latitude, longitude));
                    }
                }
            }
        }
    }
    
    [GeneratedRegex(@"\[(.*)\]")]
    private static partial Regex SectionHeaderRegex();
}