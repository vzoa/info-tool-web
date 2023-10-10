using System.Globalization;
using System.Text.RegularExpressions;
using Coravel.Invocable;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Options;
using ZoaReference.Features.Routes.Models;
using ZoaReference.Features.Routes.Repositories;

namespace ZoaReference.Features.Routes.ScheduledJobs;

public class FetchAndStoreLoaRules(ILogger<FetchAndStoreLoaRules> logger, IHttpClientFactory httpClientFactory, LoaRuleRepository loaRules, IOptionsMonitor<AppSettings> appSettings) : IInvocable
{
    public async Task Invoke()
    {
        // TODO need to add some try catch exception handling?
        
        var httpClient = httpClientFactory.CreateClient();
        await using var responseStream = await httpClient.GetStreamAsync(appSettings.CurrentValue.Urls.LoaFile);
        using var reader = new StreamReader(responseStream);
        logger.LogInformation("Fetched LOA file from: {url}", appSettings.CurrentValue.Urls.LoaFile);
        
        using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
        csv.Context.RegisterClassMap<LoaRuleMap>();
        var records = csv.GetRecords<LoaRule>().ToList();
        
        loaRules.ClearRules();
        loaRules.AddRules(records);
    }

    private class LoaRuleMap : ClassMap<LoaRule>
    {
        public LoaRuleMap()
        {
            Map(m => m.DepartureAirportRegex).Convert(args => new Regex(args.Row.GetField("Departure_Regex")));
            Map(m => m.ArrivalAirportRegex).Convert(args => new Regex(args.Row.GetField("Arrival_Regex")));
            Map(m => m.Route).Name("Route");
            Map(m => m.IsRnavRequired).Name("RNAV Required");
            Map(m => m.Notes).Name("Notes");
        }
    }
}