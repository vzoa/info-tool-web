using System.Text.Json;
using Coravel.Invocable;
using Microsoft.Extensions.Options;
using ZoaReference.Features.DigitalAtis.Models;
using ZoaReference.Features.DigitalAtis.Repositories;

namespace ZoaReference.Features.DigitalAtis.ScheduledJobs;

public class FetchAndStoreAtis(ILogger<FetchAndStoreAtis> logger, IHttpClientFactory httpClientFactory, IOptionsMonitor<AppSettings> appSettings, DigitalAtisRepository digitalAtisRepository) : IInvocable, ICancellableInvocable
{
    public CancellationToken CancellationToken { get; set; }
    
    public async Task Invoke()
    {
        // Attempt to fetch the D-ATIS JSON file. If we have an error, exit
        logger.LogInformation("Fetching D-ATIS data at: {time}", DateTime.UtcNow);
        var httpClient = httpClientFactory.CreateClient();
        Stream? stream = null;
        try
        {
            stream = await httpClient.GetStreamAsync($"{appSettings.CurrentValue.Urls.ClowdDatisApiEndpoint}/all", CancellationToken);
            logger.LogInformation("Successfully fetched D-ATIS data");
        }
        catch (Exception ex)
        {
            logger.LogError("Error fetching D-ATIS data: {error}", ex.ToString());
            if (stream is not null)
            {
                await stream.DisposeAsync();
            }
            return;
        }

        // Deserialize JSON and dispose stream now that we're done with it
        var apiAtisList = await JsonSerializer.DeserializeAsync<List<ClowdDatisDto>>(stream, cancellationToken: CancellationToken);
        await stream.DisposeAsync();

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
    }
}