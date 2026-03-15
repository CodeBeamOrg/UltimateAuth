using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Credentials.Reference;

internal sealed class PasswordCredentialProvider : ICredentialProvider
{
    private readonly IPasswordCredentialStore _store;
    private readonly ICredentialValidator _validator;

    public CredentialType Type => CredentialType.Password;

    public PasswordCredentialProvider(IPasswordCredentialStore store, ICredentialValidator validator)
    {
        _store = store;
        _validator = validator;
    }

    public async Task<IReadOnlyCollection<ICredential>> GetByUserAsync(TenantKey tenant, UserKey userKey, CancellationToken ct = default)
    {
        var creds = await _store.GetByUserAsync(tenant, userKey, ct);
        return creds.Cast<ICredential>().ToList();
    }

    public async Task<bool> ValidateAsync(ICredential credential, string secret, CancellationToken ct = default)
    {
        var result = await _validator.ValidateAsync(credential, secret, ct);
        return result.IsValid;
    }
}
