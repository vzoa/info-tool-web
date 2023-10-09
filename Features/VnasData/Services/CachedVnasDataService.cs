using System.Diagnostics;
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

    public async Task<IEnumerable<Facility>?> GetArtccFacilities(string artccId, CancellationToken c = default)
    {
        if (cache.TryGetValue<IEnumerable<Facility>>(MakeFactilityCacheKey(artccId), out var cached))
        {
            logger.LogInformation("Found cached VNAS Facilities data for {artcc}", artccId);
            return cached;
        }
        
        var jsonRoot = await GetJsonRoot(artccId, c);
        if (jsonRoot is null)
        {
            return Enumerable.Empty<Facility>();
        }

        var returnFacilities = new List<Facility>();
        var queue = new Queue<Facility>();
        queue.Enqueue(jsonRoot.Facility);

        while (queue.Count > 0)
        {
            var facility = queue.Dequeue();
            returnFacilities.Add(facility);
            facility.ChildFacilities.ForEach(child => queue.Enqueue(child));
        }

        var expiration = DateTimeOffset.UtcNow.AddSeconds(appSettings.CurrentValue.CacheTtls.VnasData);
        cache.Set(MakeFactilityCacheKey(artccId), returnFacilities, expiration);
        return returnFacilities;
    }

    public async Task<IEnumerable<VideoMap>?> GetArtccVideoMaps(string artccId, CancellationToken c = default)
    {
        if (cache.TryGetValue<IEnumerable<VideoMap>>(MakeVideoMapCacheKey(artccId), out var cached))
        {
            return cached;
        }
        
        var jsonRoot = await GetJsonRoot(artccId, c);
        if (jsonRoot is null)
        {
            return Enumerable.Empty<VideoMap>();
        }
        
        var expiration = DateTimeOffset.UtcNow.AddSeconds(appSettings.CurrentValue.CacheTtls.VnasData);
        cache.Set<ICollection<VideoMap>>(MakeVideoMapCacheKey(artccId), jsonRoot.VideoMaps, expiration);
        return jsonRoot.VideoMaps;
    }

    private async Task<VnasApiRoot?> GetJsonRoot(string artccId, CancellationToken c = default)
    {
        var httpClient = httpClientFactory.CreateClient();
        var jsonRoot = await httpClient.GetFromJsonAsync<VnasApiRoot>($"{appSettings.CurrentValue.Urls.VnasApiEndpoint}/artccs/{artccId.ToUpper()}", _jsonOptions, c);
        return jsonRoot;
    }

    public async Task ForceCache(string artccId, CancellationToken c = default)
    {
        var task1 = GetArtccFacilities(artccId, c);
        var task2 = GetArtccVideoMaps(artccId, c);
        await Task.WhenAll(task1, task2);
    }
    
    private static string MakeFactilityCacheKey(string id) => $"VnasDataFacility:{id}";
    
    private static string MakeVideoMapCacheKey(string id) => $"VnasDataVideoMap:{id}";
}
