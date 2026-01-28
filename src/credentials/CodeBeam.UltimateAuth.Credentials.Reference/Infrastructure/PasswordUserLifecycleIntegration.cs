using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Users.Abstractions;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Credentials.Reference;

internal sealed class PasswordUserLifecycleIntegration : IUserLifecycleIntegration
{
    private readonly ICredentialStore<UserKey> _credentialStore;
    private readonly IUAuthPasswordHasher _passwordHasher;
    private readonly IClock _clock;

    public PasswordUserLifecycleIntegration(ICredentialStore<UserKey> credentialStore, IUAuthPasswordHasher passwordHasher, IClock clock)
    {
        _credentialStore = credentialStore;
        _passwordHasher = passwordHasher;
        _clock = clock;
    }

    public async Task OnUserCreatedAsync(string? tenantId, UserKey userKey, object request, CancellationToken ct)
    {
        if (request is not CreateUserRequest r)
            return;

        if (string.IsNullOrWhiteSpace(r.Password))
            return;

        var hash = _passwordHasher.Hash(r.Password);

        var credential = new PasswordCredential<UserKey>(
            userId: userKey,
            loginIdentifier: r.PrimaryIdentifierValue!,
            secretHash: hash,
            security: new CredentialSecurityState(CredentialSecurityStatus.Active, null, null, null),
            metadata: new CredentialMetadata(_clock.UtcNow, _clock.UtcNow, null));

        await _credentialStore.AddAsync(tenantId, credential, ct);
    }

    public async Task OnUserDeletedAsync(string? tenantId, UserKey userKey, DeleteMode mode, CancellationToken ct)
    {
        await _credentialStore.DeleteByUserAsync(tenantId, userKey, ct);
    }
}
