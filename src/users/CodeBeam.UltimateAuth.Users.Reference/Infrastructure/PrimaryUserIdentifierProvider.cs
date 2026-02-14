using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference;

internal sealed class PrimaryUserIdentifierProvider : IPrimaryUserIdentifierProvider
{
    private readonly IUserIdentifierStore _store;

    public PrimaryUserIdentifierProvider(IUserIdentifierStore store)
    {
        _store = store;
    }

    public async Task<PrimaryUserIdentifiers?> GetAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default)
    {
        var identifiers = await _store.GetByUserAsync(tenant, userKey, ct);
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
