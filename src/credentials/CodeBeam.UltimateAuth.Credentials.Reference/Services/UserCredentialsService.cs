using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Reference.Internal;
using CodeBeam.UltimateAuth.Server.Infrastructure;

namespace CodeBeam.UltimateAuth.Credentials.Reference;

internal sealed class UserCredentialsService : IUserCredentialsService, IUserCredentialsInternalService
{
    private readonly IAccessOrchestrator _accessOrchestrator;
    private readonly ICredentialStore<UserKey> _credentials;
    private readonly ICredentialSecretStore<UserKey> _secrets;
    private readonly IUAuthPasswordHasher _hasher;
    private readonly IClock _clock;

    public UserCredentialsService(
        IAccessOrchestrator accessOrchestrator,
        ICredentialStore<UserKey> credentials,
        ICredentialSecretStore<UserKey> secrets,
        IUAuthPasswordHasher hasher,
        IClock clock)
    {
        _accessOrchestrator = accessOrchestrator;
        _credentials = credentials;
        _secrets = secrets;
        _hasher = hasher;
        _clock = clock;
    }

    public async Task<GetCredentialsResult> GetAllAsync(AccessContext context, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cmd = new GetAllCredentialsCommand(
            async innerCt =>
            {
                if (context.ActorUserKey is not UserKey userKey)
                    throw new UnauthorizedAccessException();

                var creds = await _credentials.GetByUserAsync(context.ResourceTenant, userKey, innerCt);

                var dtos = creds
                    .OfType<ICredentialDescriptor>()
                    .Select(c => new CredentialDto {
                        Type = c.Type,
                        Status = c.Security.Status,
                        CreatedAt = c.Metadata.CreatedAt,
                        LastUsedAt = c.Metadata.LastUsedAt,
                        RestrictedUntil = c.Security.RestrictedUntil,
                        ExpiresAt = c.Security.ExpiresAt,
                        Source = c.Metadata.Source})
                    .ToArray();

                return new GetCredentialsResult
                {
                    Credentials = dtos
                };
            });

        return await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }

    // ---------------- ADD ----------------

    public async Task<AddCredentialResult> AddAsync(AccessContext context, AddCredentialRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cmd = new AddCredentialCommand(
            async innerCt =>
            {
                if (context.ActorUserKey is not UserKey userKey)
                    throw new UnauthorizedAccessException();

                var exists = await _credentials.ExistsAsync(context.ResourceTenant, userKey, request.Type, innerCt);

                if (exists)
                    return AddCredentialResult.Fail("credential_already_exists");

                var hash = _hasher.Hash(request.Secret);

                var credential = new PasswordCredential<UserKey>(
                    userId: userKey,
                    loginIdentifier: userKey.Value,
                    secretHash: hash,
                    security: new CredentialSecurityState(CredentialSecurityStatus.Active),
                    metadata: new CredentialMetadata { CreatedAt = _clock.UtcNow, Source = request.Source });

                await _credentials.AddAsync(context.ResourceTenant, credential, innerCt);

                return AddCredentialResult.Success(request.Type);
            });

        return await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }

    // ---------------- CHANGE ----------------

    public async Task<ChangeCredentialResult> ChangeAsync(AccessContext context, CredentialType type, ChangeCredentialRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cmd = new ChangeCredentialCommand(
            async innerCt =>
            {
                if (context.ActorUserKey is not UserKey userKey)
                    throw new UnauthorizedAccessException();

                var hash = _hasher.Hash(request.NewSecret);

                await _secrets.SetAsync(context.ResourceTenant, userKey, type, hash, innerCt);
                return ChangeCredentialResult.Success(type);
            });

        return await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }

    // ---------------- REVOKE ----------------

    public async Task<CredentialActionResult> RevokeAsync(AccessContext context, CredentialType type, RevokeCredentialRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cmd = new RevokeCredentialCommand(
            async innerCt =>
            {
                if (context.ActorUserKey is not UserKey userKey)
                    throw new UnauthorizedAccessException();

                var security = new CredentialSecurityState(
                    CredentialSecurityStatus.Revoked,
                    restrictedUntil: request.Until,
                    expiresAt: null,
                    reason: request.Reason);

                await _credentials.UpdateSecurityStateAsync(context.ResourceTenant, userKey, type, security, innerCt);
                return CredentialActionResult.Success();
            });

        return await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }

    public async Task<CredentialActionResult> ActivateAsync(AccessContext context, CredentialType type, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cmd = new ActivateCredentialCommand(
            async innerCt =>
            {
                if (context.ActorUserKey is not UserKey userKey)
                    throw new UnauthorizedAccessException();

                var security = new CredentialSecurityState(CredentialSecurityStatus.Active);
                await _credentials.UpdateSecurityStateAsync(context.ResourceTenant, userKey, type, security, innerCt);

                return CredentialActionResult.Success();
            });

        return await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }

    public async Task BeginResetAsync(AccessContext context, CredentialType type, BeginCredentialResetRequest request, CancellationToken ct)
    {
        var cmd = new BeginCredentialResetCommand(async innerCt =>
        {
            if (context.ActorUserKey is not UserKey userKey)
                throw new UnauthorizedAccessException();

            var security = new CredentialSecurityState(CredentialSecurityStatus.ResetRequested, reason: request.Reason);

            await _credentials.UpdateSecurityStateAsync(context.ResourceTenant, userKey, type, security, innerCt);
            return CredentialActionResult.Success();
        });

        await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }

    public async Task CompleteResetAsync(AccessContext context, CredentialType type, CompleteCredentialResetRequest request, CancellationToken ct)
    {
        var cmd = new CompleteCredentialResetCommand(async innerCt =>
        {
            if (context.ActorUserKey is not UserKey userKey)
                throw new UnauthorizedAccessException();

            var hash = _hasher.Hash(request.NewSecret);

            await _secrets.SetAsync(context.ResourceTenant, userKey, type, hash, innerCt);

            var security = new CredentialSecurityState(CredentialSecurityStatus.Active);
            await _credentials.UpdateSecurityStateAsync(context.ResourceTenant, userKey, type, security, innerCt);
            return CredentialActionResult.Success();
        });

        await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }

    public async Task<CredentialActionResult> DeleteAsync(AccessContext context, CredentialType type, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cmd = new DeleteCredentialCommand(
            async innerCt =>
            {
                if (context.ActorUserKey is not UserKey userKey)
                    throw new UnauthorizedAccessException();

                await _credentials.DeleteAsync(context.ResourceTenant, userKey, type, innerCt);
                return CredentialActionResult.Success();
            });

        return await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }

    // ----------------------------------------
    // INTERNAL ONLY - NEVER CALL THEM DIRECTLY
    // ----------------------------------------
    async Task<CredentialActionResult> IUserCredentialsInternalService.DeleteInternalAsync(TenantKey tenant, UserKey userKey, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        await _credentials.DeleteByUserAsync(tenant, userKey, ct);
        return CredentialActionResult.Success();
    }
}
