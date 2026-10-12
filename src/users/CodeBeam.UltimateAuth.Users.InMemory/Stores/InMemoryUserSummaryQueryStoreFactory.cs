using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference;

namespace CodeBeam.UltimateAuth.Users.InMemory;

internal sealed class InMemoryUserSummaryQueryStoreFactory : IUserSummaryQueryStoreFactory
{
    private readonly IUserLifecycleStoreFactory _lifecycles;
    private readonly IUserProfileStoreFactory _profiles;
    private readonly IUserIdentifierStoreFactory _identifiers;
    private readonly IUAuthPaginationPolicy _pagination;

    public InMemoryUserSummaryQueryStoreFactory(IUserLifecycleStoreFactory lifecycles, IUserProfileStoreFactory profiles, IUserIdentifierStoreFactory identifiers, IUAuthPaginationPolicy pagination)
    {
        _lifecycles = lifecycles;
        _profiles = profiles;
        _identifiers = identifiers;
        _pagination = pagination;
    }

    public IUserSummaryQueryStore Create(TenantKey tenant)
    {
        return new InMemoryUserSummaryQueryStore(_lifecycles.Create(tenant), _profiles.Create(tenant), _identifiers.Create(tenant), _pagination);
    }
}
