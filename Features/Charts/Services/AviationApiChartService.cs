using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using ZoaReference.Features.Charts.Models;

namespace ZoaReference.Features.Charts.Services;

public class AviationApiChartService(ILogger<AviationApiChartService> logger, IHttpClientFactory httpClientFactory, IOptionsMonitor<AppSettings> appSettings, IMemoryCache cache)
{
    public async Task<ICollection<Chart>> GetChartsForId(string id, CancellationToken c = default)
    {
        // First check if cached. If so, we don't need to hit external API
        if (cache.TryGetValue<ICollection<Chart>>(MakeCacheKey(id), out var result))
        {
            logger.LogInformation("Found cached chart result for {id}", id);
            return result ?? new List<Chart>();
        }

        // Get Charts data from API and change to our own format
        logger.LogInformation("Did not find cached result. Fetching chart result for {id} from external API", id);
        var fetchedDtos = await GetChartsDtoForId(id, c);
        var charts = MergeChartDtos(fetchedDtos);
        
        // Cache and return
        var expiration = DateTimeOffset.UtcNow.AddSeconds(appSettings.CurrentValue.CacheTtls.Charts);
        cache.Set<ICollection<Chart>>(MakeCacheKey(id), charts, expiration);
        return charts;
    }
    
    public async Task<Dictionary<string, ICollection<Chart>>> GetChartsForIds(IEnumerable<string> ids, CancellationToken c = default)
    {
        var notCached = new List<string>();
        var returnDict = new Dictionary<string, ICollection<Chart>>();
        
        // Check list of ids to see which are cached and which are not
        foreach (var id in ids)
        {
            if (cache.TryGetValue<ICollection<Chart>>(MakeCacheKey(id), out var result))
            {
                returnDict[id] = result ?? new List<Chart>();
            }
            else
            {
                notCached.Add(id);
            }
        }
        
        // For those that are not cached, fetch new
        var fetchedDtosDict = await GetChartsDtoForIds(notCached, c);
        
        // For each airport, process, store for return and cache
        foreach (var (id, chartDtos) in fetchedDtosDict)
        {
            returnDict[id] = MergeChartDtos(chartDtos);
            var expiration = DateTimeOffset.UtcNow.AddSeconds(appSettings.CurrentValue.CacheTtls.Charts);
            cache.Set<ICollection<Chart>>(MakeCacheKey(id), returnDict[id], expiration);
        }

        return returnDict;
    }

    private static ICollection<Chart> MergeChartDtos(IEnumerable<AviationApiChartDto> chartDtos)
    {
        var chartsDict = new Dictionary<string, Chart>();
        foreach (var chartDto in chartDtos)
        {
            if (IsContinuationPage(chartDto, out var name, out var page))
            {
                if (name is not null && chartsDict.TryGetValue(name, out var existingChart))
                {
                    var newPage = new ChartPage
                    {
                        PageNumber = page ?? 1,
                        PdfName = chartDto.PdfName,
                        PdfPath = chartDto.PdfPath
                    };
                    existingChart.Pages.Add(newPage);
                }
                else if (name is not null)
                {
                    chartsDict[name] = Chart.FromAviationApiDto(chartDto, name, page ?? 1);
                }
            }
            else
            {
                chartsDict[chartDto.ChartName] = Chart.FromAviationApiDto(chartDto);
            }
        }

        return chartsDict.Values;
    }
    
    private async Task<IEnumerable<AviationApiChartDto>> GetChartsDtoForId(string id, CancellationToken c = default)
    {
        var result = await GetChartsDtoForIds([id], c);
        return result.Keys.Count > 0 ? result.Values.SelectMany(chart => chart) : Enumerable.Empty<AviationApiChartDto>();
    }
    
    private async Task<Dictionary<string, ICollection<AviationApiChartDto>>> GetChartsDtoForIds(IEnumerable<string> ids, CancellationToken c = default)
    {
        var enumerable = ids as string[] ?? ids.ToArray();
        if (enumerable.Length == 0)
        {
            return new Dictionary<string, ICollection<AviationApiChartDto>>();
        }
        
        var client = httpClientFactory.CreateClient();
        client.BaseAddress = new Uri(appSettings.CurrentValue.Urls.ChartsApiEndpoint);
        var queryStr = string.Join(",", enumerable);
        Dictionary<string, ICollection<AviationApiChartDto>>? apiJson = null;
        try
        {
            apiJson = await client.GetFromJsonAsync<Dictionary<string, ICollection<AviationApiChartDto>>>($"?apt={queryStr}", c);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning("Error while fetching charts for {airports}: {error}", queryStr, ex);
        }
        
        return apiJson ?? new Dictionary<string, ICollection<AviationApiChartDto>>();
    }

    private static bool IsContinuationPage(AviationApiChartDto chartDto, out string? name, out int? page)
    {
        name = null; 
        page = null;
        
        if (!chartDto.ChartName.Contains(", CONT."))
        {
            return false;
        }

        var split = chartDto.ChartName.Split(", CONT.");
        name = split[0];
        page = int.Parse(split[1]) + 1; // Means that "CONT.1" returns page 2
        return true;
    }

    private static string MakeCacheKey(string id) => $"ChartsCacheKey:{id.ToUpper()}";
}
