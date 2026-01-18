using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Credentials.Reference;

internal sealed class DefaultUserCredentialsService : IUserCredentialsService
{
    private readonly ICredentialStore<UserKey> _credentials;
    private readonly ICredentialSecretStore<UserKey> _secrets;
    private readonly ICredentialSecurityVersionStore<UserKey> _securityVersions;
    private readonly IUAuthPasswordHasher _hasher;
    private readonly IClock _clock;

    public DefaultUserCredentialsService(
        ICredentialStore<UserKey> credentials,
        ICredentialSecretStore<UserKey> secrets,
        ICredentialSecurityVersionStore<UserKey> securityVersions,
        IUAuthPasswordHasher hasher,
        IClock clock)
    {
        _credentials = credentials;
        _secrets = secrets;
        _securityVersions = securityVersions;
        _hasher = hasher;
        _clock = clock;
    }

    public async Task<CredentialProvisionResult> SetInitialAsync(string? tenantId, UserKey userKey, SetInitialCredentialRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var existing = await _credentials.GetByUserAsync(tenantId, userKey, ct);
        if (existing.Any(c => c.Type == request.Type))
            return CredentialProvisionResult.AlreadyExists(request.Type);

        var hash = _hasher.Hash(request.Secret);

        await _secrets.UpdateSecretAsync(
            tenantId,
            userKey,
            request.Type,
            hash,
            ct);

        return CredentialProvisionResult.Success(request.Type);
    }

    public async Task<ChangeCredentialResult> ChangeAsync(string? tenantId, UserKey userKey, ChangeCredentialRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var hash = _hasher.Hash(request.NewSecret);

        await _secrets.UpdateSecretAsync(
            tenantId,
            userKey,
            request.Type,
            hash,
            ct);

        await _securityVersions.IncrementAsync(tenantId, userKey, ct);

        return ChangeCredentialResult.Success(request.Type);
    }

    public async Task ResetAsync(string? tenantId, UserKey userKey, ResetPasswordRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var hash = _hasher.Hash(request.NewPassword);

        await _secrets.UpdateSecretAsync(tenantId, userKey, CredentialType.Password, hash, ct);

        await _securityVersions.IncrementAsync(tenantId, userKey, ct);
    }

    public Task RevokeAllAsync(string? tenantId, UserKey userKey, CancellationToken ct = default)
        => _securityVersions.IncrementAsync(tenantId, userKey, ct);

    public Task DeleteAllAsync(string? tenantId, UserKey userKey, CancellationToken ct = default)
        => _credentials.DeleteByUserAsync(tenantId, userKey, ct);
}
