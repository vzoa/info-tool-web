using System.Text.Json;
using Microsoft.Extensions.Options;
using ZoaReference.Features.DigitalAtis.Models;
using ZoaReference.Features.DigitalAtis.Repositories;

namespace ZoaReference.Features.DigitalAtis.Services;

public class DigitalAtisBackgroundService(ILogger<DigitalAtisBackgroundService> logger,
        DigitalAtisRepository digitalAtisRepository,
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<AppSettings> appSettings)
    : BackgroundService
{
    private int _delaySeconds = appSettings.CurrentValue.DigitalAtisRefreshSeoncds;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var firstLoop = true;
        while (!stoppingToken.IsCancellationRequested)
        {
            // Delay at beginning of the loop, but skip if first time through the loop
            if (!firstLoop)
            {
                logger.LogInformation("Pausing D-ATIS Worker for {time} seconds", _delaySeconds);
                await Task.Delay(_delaySeconds * 1000, stoppingToken);
            }
            firstLoop = false;

            // Attempt to fetch the D-ATIS JSON file. If we have an error, retry in 1 second
            logger.LogInformation("Fetching D-ATIS data at: {time}", DateTime.UtcNow);
            var httpClient = httpClientFactory.CreateClient();
            Stream stream;
            try
            {
                stream = await httpClient.GetStreamAsync($"{appSettings.CurrentValue.Urls.ClowdDatisApiEndpoint}/all", stoppingToken);
                logger.LogInformation("Successfully fetched D-ATIS data");
            }
            catch (Exception ex)
            {
                logger.LogError("Error fetching D-ATIS data: {error}", ex.ToString());
                _delaySeconds = 1;
                break;
            }

            // Deserialize JSON and dispose stream now that we're done with it
            var apiAtisList = await JsonSerializer.DeserializeAsync<List<ClowdDatisDto>>(stream, cancellationToken: stoppingToken);
            stream.Dispose();

            foreach (var atis in apiAtisList ?? Enumerable.Empty<ClowdDatisDto>())
            {
                if (!Atis.TryParseFromClowdAtis(atis, out var fetchedAtis))
                {
                    logger.LogError("Error parsing D-ATIS for {id}: {raw}", atis.Airport, atis.Datis);
                    continue;
                }
                
                // If we don't have any ATIS for this airport
                if (!digitalAtisRepository.TryGetAtisForId(fetchedAtis!.IcaoId, out var atisRecord))
                {
                    var newRecord = fetchedAtis!.Type switch
                    {
                        Atis.AtisType.Combined => new DigitalAtisRecord(fetchedAtis!.IcaoId, fetchedAtis!, null,
                            null),
                        Atis.AtisType.Departure => new DigitalAtisRecord(fetchedAtis!.IcaoId, null, fetchedAtis!,
                            null),
                        Atis.AtisType.Arrival => new DigitalAtisRecord(fetchedAtis!.IcaoId, null, null,
                            fetchedAtis!),
                    };
                    digitalAtisRepository.UpdateAtisForId(fetchedAtis!.IcaoId, newRecord);
                    logger.LogInformation("Found new D-ATIS Airport: {id}", fetchedAtis!.IcaoId);
                }
                // Else, update existing record
                else
                {
                    var updatedRecord = fetchedAtis!.Type switch
                    {
                        Atis.AtisType.Combined => atisRecord with { Combined = fetchedAtis },
                        Atis.AtisType.Departure => atisRecord with { Departure = fetchedAtis },
                        Atis.AtisType.Arrival => atisRecord with { Arrival = fetchedAtis },
                    };
                    digitalAtisRepository.UpdateAtisForId(fetchedAtis!.IcaoId, updatedRecord);
                }
            }

            _delaySeconds = appSettings.CurrentValue.DigitalAtisRefreshSeoncds;
        }
    }
}
