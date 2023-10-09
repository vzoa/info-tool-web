using System.Globalization;
using Coravel.Invocable;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Options;
using ZoaReference.Features.IcaoReference.Models;
using ZoaReference.Features.IcaoReference.Repositories;

namespace ZoaReference.Features.IcaoReference.ScheduledJobs;

public class FetchAndStoreAircraftInfo(ILogger<FetchAndStoreAircraftInfo> logger, IHttpClientFactory httpClientFactory,
        IOptionsMonitor<AppSettings> appSettings, AircraftTypeRepository aircraftTypeRepository)
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

        // Setup reads and DB context
        var client = httpClientFactory.CreateClient();
        await using var responseStream = await client.GetStreamAsync(appSettings.CurrentValue.Urls.AircraftCsv);
        using var reader = new StreamReader(responseStream);
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        logger.LogInformation("Fetched Aircraft ICAO CSV file from: {url}", appSettings.CurrentValue.Urls.AircraftCsv);

        // Read the file as objects
        csv.Context.RegisterClassMap<CsvAircraftMap>();
        var records = csv.GetRecords<AircraftType>().ToArray();

        // Delete old data
        aircraftTypeRepository.ClearAircraftTypes();
        logger.LogInformation("Deleted records from Aircraft database");

        // Add new data
        aircraftTypeRepository.AddAircraftTypes(records);
        logger.LogInformation("Added {num} records to Aircraft database", records.Length);
    }

    private class CsvAircraftMap : ClassMap<AircraftType>
    {
        public CsvAircraftMap()
        {
            Map(m => m.IcaoId).Name("Type Designator");
            Map(m => m.Manufacturer).Name("Manufacturer");
            Map(m => m.Model).Name("Model");
            Map(m => m.Class).Name("Description");
            Map(m => m.EngineType).Name("Engine Type");
            Map(m => m.EngineCount).Name("Engine Count");
            Map(m => m.IcaoWakeTurbulenceCategory).Name("WTC");
            Map(m => m.FaaEngineNumberType).Name("Engine Number-Type");
            Map(m => m.FaaWeightClass).Name("FAA Weight Class");
            Map(m => m.ConsolidatedWakeTurbulenceCategory).Name("CWT");
            Map(m => m.SameRunwaySeparationCategory).Name("SRS");
            Map(m => m.LandAndHoldShortGroup).Name("LAHSO");
        }
    }
}
