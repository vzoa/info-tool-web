using System.Globalization;
using Coravel.Invocable;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Options;
using ZoaReference.Features.IcaoReference.Models;
using ZoaReference.Features.IcaoReference.Repositories;

namespace ZoaReference.Features.IcaoReference.ScheduledJobs;

public class FetchAndStoreAirlineIcao(ILogger<FetchAndStoreAirlineIcao> logger, IHttpClientFactory httpClientFactory,
        IOptionsMonitor<AppSettings> appSettings, AirlineRepository airlineRepository)
    : IInvocable
{
    public async Task Invoke()
    {
        //TODO ADD TRY CATCH and logging

        // Setup reads and DB context
        //try
        //{

        //}
        //catch (Exception ex)
        //{
        //	_logger.LogError("Error while trying to fetch and read Aircraft ICAO csv: {ex}", ex.ToString());
        //	return;
        //}

        // Setup reads
        var url = appSettings.CurrentValue.Urls.AirlinesCsv;
        var client = httpClientFactory.CreateClient();
        await using var responseStream = await client.GetStreamAsync(url);
        using var reader = new StreamReader(responseStream);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        logger.LogInformation("Fetched Airline ICAO CSV file from: {url}", url);

        // Read the file as objects
        csv.Context.RegisterClassMap<CsvAirlineMap>();
        var records = csv.GetRecords<Airline>().ToArray();

        // Delete old data
        airlineRepository.ClearAirlines();
        logger.LogInformation("Deleted records from Airlines repository");

        // Add new data
        airlineRepository.AddAirlines(records);
        logger.LogInformation("Added {num} records to Airlines database", records.Length);
    }

    private class CsvAirlineMap : ClassMap<Airline>
    {
        public CsvAirlineMap()
        {
            Map(m => m.IcaoId).Name("3Ltr");
            Map(m => m.Name).Name("Company");
            Map(m => m.Callsign).Name("Telephony");
            Map(m => m.Country).Name("Country");
        }
    }
}
