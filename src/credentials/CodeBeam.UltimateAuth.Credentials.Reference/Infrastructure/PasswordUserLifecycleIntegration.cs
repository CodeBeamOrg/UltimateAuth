using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Users;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Credentials.Reference;

internal sealed class PasswordUserLifecycleIntegration : IUserLifecycleIntegration
{
    private readonly IPasswordCredentialStoreFactory _credentialStoreFactory;
    private readonly IUAuthPasswordHasher _passwordHasher;
    private readonly IClock _clock;

    public PasswordUserLifecycleIntegration(IPasswordCredentialStoreFactory credentialStoreFactory, IUAuthPasswordHasher passwordHasher, IClock clock)
    {
        _credentialStoreFactory = credentialStoreFactory;
        _passwordHasher = passwordHasher;
        _clock = clock;
    }

    public async Task OnUserCreatedAsync(TenantKey tenant, UserKey userKey, object request, CancellationToken ct)
    {
        if (request is not CreateUserRequest r)
            return;

        if (string.IsNullOrWhiteSpace(r.Password))
            return;

        var hash = _passwordHasher.Hash(r.Password);

        var credential = PasswordCredential.Create(
            id: null,
            tenant: tenant,
            userKey: userKey,
            secretHash: hash,
            security: CredentialSecurityState.Active(),
            metadata: new CredentialMetadata { },
            _clock.UtcNow);

        var credentialStore = _credentialStoreFactory.Create(tenant);
        await credentialStore.AddAsync(credential, ct);
    }

    public async Task OnUserDeletedAsync(TenantKey tenant, UserKey userKey, DeleteMode mode, CancellationToken ct)
    {
        var credentialStore = _credentialStoreFactory.Create(tenant);
        await credentialStore.DeleteByUserAsync(userKey, mode, _clock.UtcNow, ct);
    }
}
