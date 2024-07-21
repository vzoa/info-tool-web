using ZoaReference.Features.Routes.Models;

namespace ZoaReference.Features.Routes.Repositories;

public class AliasRouteRuleRepository
{
    private List<AliasRouteRule> _repository = [];

    public void AddRule(AliasRouteRule rule) => _repository.Add(rule);
    
    public void AddRules(IEnumerable<AliasRouteRule> rules) => _repository.AddRange(rules);

    public IEnumerable<AliasRouteRule> GetAllRules() => _repository;

    public void ClearRules() => _repository.Clear();
}