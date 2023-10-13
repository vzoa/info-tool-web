using System.Text.RegularExpressions;
using Coravel.Invocable;
using Microsoft.Extensions.Options;
using ZoaReference.Features.Routes.Models;
using ZoaReference.Features.Routes.Repositories;

namespace ZoaReference.Features.Routes.ScheduledJobs;

public partial class FetchAndStoreAliasRoutes(ILogger<FetchAndStoreAliasRoutes> logger,
        IHttpClientFactory httpClientFactory, IOptionsMonitor<AppSettings> appSettings, AliasRouteRuleRepository routeRuleRepository)
    : IInvocable
{
    public async Task Invoke()
    {
        // TODO need to add some try catch exception handling?

        var httpClient = httpClientFactory.CreateClient();
        await using var responseStream = await httpClient.GetStreamAsync(appSettings.CurrentValue.Urls.AliasTextFile);
        using var reader = new StreamReader(responseStream);
        logger.LogInformation("Fetched ZOA Alias file from: {url}", appSettings.CurrentValue.Urls.AliasTextFile);

        var rules = new List<AliasRouteRule>();
        while (!reader.EndOfStream)
        {
            var line = reader.ReadLine();
            if (line is not null && TryParseRouteRule(line, out var routeRule))
            {
                rules.Add(routeRule);
            }
        }
        routeRuleRepository.ClearRules();
        routeRuleRepository.AddRules(rules);
    }

    private static bool TryParseRouteRule(string line, out AliasRouteRule? routeRule)
    {
        routeRule = null;

        // Return early if the given line is null or not a route command
        if (!AmRteRegex().IsMatch(line))
        {
            return false;
        }

        var commandMatch = CommandNameRegex().Match(line);
        var routeMatch = RouteRegex().Match(line);
        
        // Return early if can't match
        if (!commandMatch.Success || !routeMatch.Success)
        {
            return false;
        }
        
        routeRule = new AliasRouteRule
        {
            DepartureAirport = commandMatch.Groups[1].Value.ToUpper(),
            DepartureRunway = string.IsNullOrEmpty(commandMatch.Groups[2].Value) ? null : int.Parse(commandMatch.Groups[2].Value),
            ArrivalAirport = commandMatch.Groups[3].Value.ToUpper(),
            ArrivalRunway = string.IsNullOrEmpty(commandMatch.Groups[4].Value) ? null : int.Parse(commandMatch.Groups[4].Value),
            AllowedAircraftTypes = string.IsNullOrEmpty(commandMatch.Groups[5].Value)
                ? AliasRouteRule.RouteAircraftTypes.Jet | AliasRouteRule.RouteAircraftTypes.Turboprop | AliasRouteRule.RouteAircraftTypes.Prop
                : AliasRouteRule.StringToType(commandMatch.Groups[5].Value),
            Route = routeMatch.Groups[2].Value.Trim()
        };
        
        return true;
    }

    [GeneratedRegex(@"\.am rte")]
    private static partial Regex AmRteRegex();

    [GeneratedRegex(@"([a-zA-Z0-9]{3})([0-9]{0,2})([a-zA-Z0-9]{3})([0-9]{0,2})([TPJtpj]?)")]
    private static partial Regex CommandNameRegex();

    [GeneratedRegex(@"\.am rte (\$route)?([^\$]*)(\$route)?")]
    private static partial Regex RouteRegex();
}
