using Coravel.Invocable;
using Microsoft.Extensions.Options;
using ZoaReference.Features.Scratchpads.Models;
using ZoaReference.Features.Scratchpads.Repositories;

namespace ZoaReference.Features.Scratchpads.ScheduledJobs;


public class FetchAndStoreScratchpads(
    ILogger<FetchAndStoreScratchpads> logger,
    IHttpClientFactory httpClientFactory,
    IOptionsMonitor<AppSettings> appSettings,
    ScratchpadsRepository scratchpadsRepository)
    : IInvocable
{
    public async Task Invoke()
    {
        var url = appSettings.CurrentValue.Urls.ScratchpadsJson;
        try
        {
            logger.LogInformation("Starting scratchpad fetch and update task");
            using var httpClient = httpClientFactory.CreateClient();
            var scratchpads = await httpClient.GetFromJsonAsync<List<AirportScratchpad>>(url);

            if (scratchpads is null)
            {
                logger.LogWarning("Error while fetching scratchpads: null JSON deserialization from {url}", url);
                return;
            }

            logger.LogInformation("Successfully fetched scratchpads from {url}", url);

            scratchpadsRepository.ClearAirports();
            logger.LogInformation("Deleted all scratchpads");

            var count = 0;
            foreach (var airport in scratchpads)
            {
                if (!scratchpadsRepository.TryAddScratchpads(airport.Id, airport.Scratchpads))
                {
                    logger.LogWarning("Error adding {id} to Scratchpad Repository", airport.Id);
                    continue;
                }

                count += 1;
            }
            
            logger.LogInformation("Added {num} airport scratchpad definitions to Scratchpad Repository", count);
        }
        catch (Exception e)
        {
            logger.LogWarning("Exception while trying to fetch and update scratchpads: {ex}", e);
        }
    }
}
