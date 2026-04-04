using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Credentials.Reference;

internal sealed class PasswordCredentialProvider : ICredentialProvider
{
    private readonly IPasswordCredentialStoreFactory _storeFactory;
    private readonly ICredentialValidator _validator;

    public CredentialType Type => CredentialType.Password;

    public PasswordCredentialProvider(IPasswordCredentialStoreFactory storeFactory, ICredentialValidator validator)
    {
        _storeFactory = storeFactory;
        _validator = validator;
    }

    public async Task<IReadOnlyCollection<ICredential>> GetByUserAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default)
    {
        var store = _storeFactory.Create(tenant);
        var creds = await store.GetByUserAsync(userKey, ct);
        return creds.Cast<ICredential>().ToList();
    }

    public async Task<bool> ValidateAsync(ICredential credential, string secret, CancellationToken ct = default)
    {
        var result = await _validator.ValidateAsync(credential, secret, ct);
        return result.IsValid;
    }
}
