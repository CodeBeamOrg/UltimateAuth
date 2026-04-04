using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference;

internal sealed class PrimaryUserIdentifierProvider : IPrimaryUserIdentifierProvider
{
    private readonly IUserIdentifierStoreFactory _storeFactory;

    public PrimaryUserIdentifierProvider(IUserIdentifierStoreFactory storeFactory)
    {
        _storeFactory = storeFactory;
    }

    public async Task<PrimaryUserIdentifiers?> GetAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default)
    {
        var store = _storeFactory.Create(tenant);
        var identifiers = await store.GetByUserAsync(userKey, ct);
        var primary = identifiers.Where(x => x.IsPrimary).ToList();

        if (primary.Count == 0)
            return null;

        return new PrimaryUserIdentifiers
        {
            UserName = primary.FirstOrDefault(x => x.Type == UserIdentifierType.Username)?.Value,
            Email = primary.FirstOrDefault(x => x.Type == UserIdentifierType.Email)?.Value,
            Phone = primary.FirstOrDefault(x => x.Type == UserIdentifierType.Phone)?.Value
        };
    }
}
