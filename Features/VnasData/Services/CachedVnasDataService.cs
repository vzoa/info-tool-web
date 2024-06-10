using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using ZoaReference.Features.VnasData.Models;

namespace ZoaReference.Features.VnasData.Services;

public class CachedVnasDataService(IMemoryCache cache, IHttpClientFactory httpClientFactory, IOptionsMonitor<AppSettings> appSettings, ILogger<CachedVnasDataService> logger)
{
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public async Task<IEnumerable<FacilityExtended>> GetArtccFacilities(string artccId, CancellationToken c = default)
    {
        if (cache.TryGetValue<IEnumerable<FacilityExtended>>(MakeFacilityCacheKey(artccId), out var cached))
        {
            logger.LogInformation("Found cached VNAS Facilities data for {artcc}", artccId);
            return cached ?? Enumerable.Empty<FacilityExtended>();
        }
        
        var jsonRoot = await GetJsonRoot(artccId, c);
        if (jsonRoot is null)
        {
            return Enumerable.Empty<FacilityExtended>();
        }
        
        var videoMapDict = jsonRoot.VideoMaps.ToDictionary(m => m.Id);
        var returnFacilities = new List<FacilityExtended>();
        var queue = new Queue<Facility>();
        queue.Enqueue(jsonRoot.Facility);

        while (queue.Count > 0)
        {
            var facility = queue.Dequeue();
            var maps = facility.StarsConfiguration?.VideoMapIds.Select(m => videoMapDict.GetValueOrDefault(m)).Where(m => m is not null);
            returnFacilities.Add(new FacilityExtended(facility, (maps ?? [])!));
            facility.ChildFacilities.ForEach(child => queue.Enqueue(child));
        }

        var expiration = DateTimeOffset.UtcNow.AddSeconds(appSettings.CurrentValue.CacheTtls.VnasData);
        cache.Set(MakeFacilityCacheKey(artccId), returnFacilities, expiration);
        return returnFacilities;
    }

    private async Task<VnasApiRoot?> GetJsonRoot(string artccId, CancellationToken c = default)
    {
        var httpClient = httpClientFactory.CreateClient();
        var jsonRoot = await httpClient.GetFromJsonAsync<VnasApiRoot>($"{appSettings.CurrentValue.Urls.VnasApiEndpoint}/artccs/{artccId.ToUpper()}", _jsonOptions, c);
        return jsonRoot;
    }

    public Task ForceCache(string artccId, CancellationToken c = default) => GetArtccFacilities(artccId, c);
    
    private static string MakeFacilityCacheKey(string id) => $"VnasDataFacility:{id}";
}
