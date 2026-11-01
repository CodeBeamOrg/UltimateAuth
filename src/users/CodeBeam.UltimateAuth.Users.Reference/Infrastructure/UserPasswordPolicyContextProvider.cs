using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Policies;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference;

internal sealed class UserPasswordPolicyContextProvider(IUserIdentifierStoreFactory storeFactory) : IPasswordPolicyContextProvider
{
    public async Task<PasswordPolicyContext> CreateAsync(TenantKey tenant, UserKey userKey, string password, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var store = storeFactory.Create(tenant);

        var identifiers = await store.GetByUserAsync(userKey, ct);

        var active = identifiers.Where(x => !x.IsDeleted).ToArray();

        var userName = active
            .Where(x => x.Type == UserIdentifierType.Username)
            .OrderByDescending(x => x.IsPrimary)
            .Select(x => x.Value)
            .FirstOrDefault();

        var values = active
            .Select(x => x.Value)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new PasswordPolicyContext
        {
            Password = password,
            UserName = userName,
            UserIdentifiers = values
        };
    }
}
