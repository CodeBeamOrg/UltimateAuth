using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Reference.Internal;
using CodeBeam.UltimateAuth.Server.Infrastructure;

namespace CodeBeam.UltimateAuth.Credentials.Reference;

internal sealed class CredentialManagementService : ICredentialManagementService, IUserCredentialsInternalService
{
    private readonly IAccessOrchestrator _accessOrchestrator;
    private readonly ICredentialStore _credentials;
    private readonly IUAuthPasswordHasher _hasher;
    private readonly IClock _clock;

    public CredentialManagementService(
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

    public async Task<GetCredentialsResult> GetAllAsync(
    AccessContext context,
    CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cmd = new AccessCommand<GetCredentialsResult>(async innerCt =>
        {
            var subjectUser = context.GetTargetUserKey();
            var now = _clock.UtcNow;

            var credentials = await _credentials.GetByUserAsync(context.ResourceTenant, subjectUser, innerCt);

            var dtos = credentials
                .OfType<ICredentialDescriptor>()
                .Select(c => new CredentialDto
                {
                    Id = c.Id,
                    Type = c.Type,
                    Status = c.Security.Status(now),
                    LockedUntil = c.Security.LockedUntil,
                    ExpiresAt = c.Security.ExpiresAt,
                    RevokedAt = c.Security.RevokedAt,
                    ResetRequestedAt = c.Security.ResetRequestedAt,
                    LastUsedAt = c.Metadata.LastUsedAt,
                    Source = c.Metadata.Source
                })
                .ToArray();

            return new GetCredentialsResult { Credentials = dtos };
        });

        return await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }

    public async Task<AddCredentialResult> AddAsync(AccessContext context, AddCredentialRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cmd = new AccessCommand<AddCredentialResult>(async innerCt =>
        {
            var subjectUser = context.GetTargetUserKey();
            var now = _clock.UtcNow;

            var hash = _hasher.Hash(request.Secret);

            var credential = PasswordCredentialFactory.Create(
                tenant: context.ResourceTenant,
                userKey: subjectUser,
                secretHash: hash,
                source: request.Source,
                now: now);

            await _credentials.AddAsync(context.ResourceTenant, credential, innerCt);

            return AddCredentialResult.Success(credential.Id, credential.Type);
        });

        return await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }

    public async Task<ChangeCredentialResult> ChangeSecretAsync(AccessContext context, ChangeCredentialRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cmd = new AccessCommand<ChangeCredentialResult>(async innerCt =>
        {
            var subjectUser = context.GetTargetUserKey();
            var now = _clock.UtcNow;

            var credential = await _credentials.GetByIdAsync(context.ResourceTenant, request.Id, innerCt);

            if (credential is not PasswordCredential pwd)
                return ChangeCredentialResult.Fail("credential_not_found");

            if (pwd.UserKey != subjectUser)
                return ChangeCredentialResult.Fail("credential_not_found");

            var verified = _hasher.Verify(pwd.SecretHash, request.CurrentSecret);
            if (!verified)
                return ChangeCredentialResult.Fail("invalid_credentials");

            var oldVersion = pwd.Version;
            var newHash = _hasher.Hash(request.NewSecret);
            pwd.ChangeSecret(newHash, now);
            await _credentials.UpdateAsync(context.ResourceTenant, pwd, oldVersion, innerCt);

            return ChangeCredentialResult.Success(credential.Type);
        });

        return await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }

    public async Task<CredentialActionResult> RevokeAsync(AccessContext context, RevokeCredentialRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cmd = new AccessCommand<CredentialActionResult>(async innerCt =>
        {
            var subjectUser = context.GetTargetUserKey();
            var now = _clock.UtcNow;

            var credential = await _credentials.GetByIdAsync(context.ResourceTenant, request.Id, innerCt);

            if (credential is not PasswordCredential pwd)
                return CredentialActionResult.Fail("credential_not_found");

            if (pwd.UserKey != subjectUser)
                return CredentialActionResult.Fail("credential_not_found");

            var oldVersion = pwd.Version;
            pwd.Revoke(now);
            await _credentials.UpdateAsync(context.ResourceTenant, pwd, oldVersion, innerCt);

            return CredentialActionResult.Success();
        });

        return await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }

    public async Task<CredentialActionResult> BeginResetAsync(AccessContext context, BeginCredentialResetRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cmd = new AccessCommand<CredentialActionResult>(async innerCt =>
        {
            var subjectUser = context.GetTargetUserKey();
            var now = _clock.UtcNow;

            var credential = await _credentials.GetByIdAsync(context.ResourceTenant, request.Id, innerCt);

            if (credential is not PasswordCredential pwd)
                return CredentialActionResult.Fail("credential_not_found");

            if (pwd.UserKey != subjectUser)
                return CredentialActionResult.Fail("credential_not_found");

            var oldVersion = pwd.Version;
            //pwd.BeginReset(now, request.Validity);
            await _credentials.UpdateAsync(context.ResourceTenant, pwd, oldVersion, innerCt);

            return CredentialActionResult.Success();
        });

        return await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }

    public async Task<CredentialActionResult> CompleteResetAsync(AccessContext context, CompleteCredentialResetRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cmd = new AccessCommand<CredentialActionResult>(async innerCt =>
        {
            var subjectUser = context.GetTargetUserKey();
            var now = _clock.UtcNow;

            var credential = await _credentials.GetByIdAsync(context.ResourceTenant, request.Id, innerCt);

            if (credential is not PasswordCredential pwd)
                return CredentialActionResult.Fail("credential_not_found");

            if (pwd.UserKey != subjectUser)
                return CredentialActionResult.Fail("credential_not_found");

            var oldVersion = pwd.Version;

            pwd.CompleteReset(now);

            var hash = _hasher.Hash(request.NewSecret);
            pwd.ChangeSecret(hash, now);
            await _credentials.UpdateAsync(context.ResourceTenant, pwd, oldVersion, innerCt);

            return CredentialActionResult.Success();
        });

        return await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }

    public async Task<CredentialActionResult> DeleteAsync(AccessContext context, DeleteCredentialRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cmd = new AccessCommand<CredentialActionResult>(async innerCt =>
        {
            var subjectUser = context.GetTargetUserKey();
            var now = _clock.UtcNow;

            var credential = await _credentials.GetByIdAsync(context.ResourceTenant, request.Id, innerCt);

            if (credential is not PasswordCredential pwd)
                return CredentialActionResult.Fail("credential_not_found");

            if (pwd.UserKey != subjectUser)
                return CredentialActionResult.Fail("credential_not_found");

            var oldVersion = pwd.Version;
            await _credentials.DeleteAsync(context.ResourceTenant, pwd.Id, request.Mode, now, oldVersion, innerCt);

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
}
