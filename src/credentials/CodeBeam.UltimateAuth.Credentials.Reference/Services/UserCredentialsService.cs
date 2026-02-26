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
    private readonly ICredentialStore _credentials;
    private readonly IUAuthPasswordHasher _hasher;
    private readonly IClock _clock;

    public UserCredentialsService(
        IAccessOrchestrator accessOrchestrator,
        ICredentialStore credentials,
        IUAuthPasswordHasher hasher,
        IClock clock)
    {
        _accessOrchestrator = accessOrchestrator;
        _credentials = credentials;
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
                        Status = c.Security.Status(_clock.UtcNow),
                        LastUsedAt = c.Metadata.LastUsedAt,
                        LockedUntil = c.Security.LockedUntil,
                        ExpiresAt = c.Security.ExpiresAt,
                        RevokedAt = c.Security.RevokedAt,
                        ResetRequestedAt = c.Security.ResetRequestedAt,
                        Source = c.Metadata.Source})
                    .ToArray();

                return new GetCredentialsResult
                {
                    Credentials = dtos
                };
            });

        return await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }

    public async Task<AddCredentialResult> AddAsync(AccessContext context, AddCredentialRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cmd = new AddCredentialCommand(async innerCt =>
        {
            var userKey = EnsureActor(context);
            var now = _clock.UtcNow;

            var alreadyHasType = (await _credentials.GetByUserAsync(context.ResourceTenant, userKey, innerCt))
                .OfType<ICredentialDescriptor>()
                .Any(c => c.Type == request.Type);

            if (alreadyHasType)
                return AddCredentialResult.Fail("credential_already_exists");

            var hash = _hasher.Hash(request.Secret);

            var credential = PasswordCredentialFactory.Create(
                tenant: context.ResourceTenant,
                userKey: userKey,
                secretHash: hash,
                source: request.Source,
                now: now);

            await _credentials.AddAsync(context.ResourceTenant, credential, innerCt);

            return AddCredentialResult.Success(request.Type);
        });

        return await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }


    public async Task<ChangeCredentialResult> ChangeAsync(AccessContext context, CredentialType type, ChangeCredentialRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cmd = new ChangeCredentialCommand(async innerCt =>
        {
            var userKey = EnsureActor(context);
            var now = _clock.UtcNow;

            var cred = await GetSingleByTypeAsync(context.ResourceTenant, userKey, type, innerCt);
            if (cred is null)
                return ChangeCredentialResult.Fail("credential_not_found");

            if (cred is PasswordCredential pwd)
            {
                var hash = _hasher.Hash(request.NewSecret);
                pwd.ChangeSecret(hash, now);
                await _credentials.UpdateAsync(context.ResourceTenant, pwd, innerCt);
            }
            else
            {
                return ChangeCredentialResult.Fail("credential_type_unsupported");
            }

            return ChangeCredentialResult.Success(type);
        });

        return await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }


    // ---------------- REVOKE ----------------

    public async Task<CredentialActionResult> RevokeAsync(AccessContext context, CredentialType type, RevokeCredentialRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cmd = new RevokeCredentialCommand(async innerCt =>
        {
            var userKey = EnsureActor(context);
            var now = _clock.UtcNow;

            var cred = await GetSingleByTypeAsync(context.ResourceTenant, userKey, type, innerCt);
            if (cred is null)
                return CredentialActionResult.Fail("credential_not_found");

            await _credentials.RevokeAsync(context.ResourceTenant, GetId(cred), now, innerCt);
            return CredentialActionResult.Success();
        });

        return await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }

    public async Task<CredentialActionResult> ActivateAsync(AccessContext context, CredentialType type, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cmd = new ActivateCredentialCommand(async innerCt =>
        {
            var userKey = EnsureActor(context);
            var now = _clock.UtcNow;

            var cred = await GetSingleByTypeAsync(context.ResourceTenant, userKey, type, innerCt);
            if (cred is null)
                return CredentialActionResult.Fail("credential_not_found");

            if (cred is ICredentialDescriptor desc && cred is PasswordCredential pwd)
            {
                pwd.UpdateSecurity(CredentialSecurityState.Active(pwd.Security.SecurityStamp), now);
                await _credentials.UpdateAsync(context.ResourceTenant, pwd, innerCt);
                return CredentialActionResult.Success();
            }

            return CredentialActionResult.Fail("credential_type_unsupported");
        });

        return await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }

    public async Task BeginResetAsync(AccessContext context, CredentialType type, BeginCredentialResetRequest request, CancellationToken ct)
    {
        var cmd = new BeginCredentialResetCommand(async innerCt =>
        {
            var userKey = EnsureActor(context);
            var now = _clock.UtcNow;

            var cred = await GetSingleByTypeAsync(context.ResourceTenant, userKey, type, innerCt);
            if (cred is null)
                return CredentialActionResult.Fail("credential_not_found");

            if (cred is PasswordCredential pwd)
            {
                pwd.UpdateSecurity(pwd.Security.BeginReset(now), now);
                await _credentials.UpdateAsync(context.ResourceTenant, pwd, innerCt);
                return CredentialActionResult.Success();
            }

            return CredentialActionResult.Fail("credential_type_unsupported");
        });

        await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }

    public async Task CompleteResetAsync(AccessContext context, CredentialType type, CompleteCredentialResetRequest request, CancellationToken ct)
    {
        var cmd = new CompleteCredentialResetCommand(async innerCt =>
        {
            var userKey = EnsureActor(context);
            var now = _clock.UtcNow;

            var cred = await GetSingleByTypeAsync(context.ResourceTenant, userKey, type, innerCt);
            if (cred is null)
                return CredentialActionResult.Fail("credential_not_found");

            if (cred is PasswordCredential pwd)
            {
                var hash = _hasher.Hash(request.NewSecret);
                pwd.ChangeSecret(hash, now);
                pwd.UpdateSecurity(pwd.Security.CompleteReset(), now);

                await _credentials.UpdateAsync(context.ResourceTenant, pwd, innerCt);
                return CredentialActionResult.Success();
            }

            return CredentialActionResult.Fail("credential_type_unsupported");
        });

        await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }

    public async Task<CredentialActionResult> DeleteAsync(AccessContext context, CredentialType type, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cmd = new DeleteCredentialCommand(async innerCt =>
        {
            var userKey = EnsureActor(context);
            var now = _clock.UtcNow;

            var cred = await GetSingleByTypeAsync(context.ResourceTenant, userKey, type, innerCt);
            if (cred is null)
                return CredentialActionResult.Fail("credential_not_found");

            await _credentials.DeleteAsync(
                tenant: context.ResourceTenant,
                credentialId: GetId(cred),
                mode: DeleteMode.Soft,
                now: now,
                ct: innerCt);

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

        await _credentials.DeleteByUserAsync(tenant, userKey, DeleteMode.Soft, _clock.UtcNow, ct);
        return CredentialActionResult.Success();
    }


    private static UserKey EnsureActor(AccessContext context)
        => context.ActorUserKey is UserKey uk ? uk : throw new UnauthorizedAccessException();

    private static Guid GetId(ICredential c)
        => c switch
        {
            PasswordCredential p => p.Id,
            _ => throw new NotSupportedException("credential_id_missing")
        };

    private async Task<ICredential?> GetSingleByTypeAsync(TenantKey tenant, UserKey userKey, CredentialType type, CancellationToken ct)
    {
        var creds = await _credentials.GetByUserAsync(tenant, userKey, ct);

        var found = creds.OfType<ICredentialDescriptor>().FirstOrDefault(x => x.Type == type);

        return found as ICredential;
    }
}
