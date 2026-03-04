using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Reference.Internal;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Users;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Credentials.Reference;

// TODO: Add unlock credential factor as admin action.
internal sealed class CredentialManagementService : ICredentialManagementService, IUserCredentialsInternalService
{
    private readonly IAccessOrchestrator _accessOrchestrator;
    private readonly ICredentialStore _credentials;
    private readonly IAuthenticationSecurityManager _authenticationSecurityManager;
    private readonly IOpaqueTokenGenerator _tokenGenerator;
    private readonly INumericCodeGenerator _numericCodeGenerator;
    private readonly IUAuthPasswordHasher _hasher;
    private readonly ITokenHasher _tokenHasher;
    private readonly ILoginIdentifierResolver _identifierResolver;
    private readonly UAuthServerOptions _options;
    private readonly IClock _clock;

    public CredentialManagementService(
        IAccessOrchestrator accessOrchestrator,
        ICredentialStore credentials,
        IAuthenticationSecurityManager authenticationSecurityManager,
        IOpaqueTokenGenerator tokenGenerator,
        INumericCodeGenerator numericCodeGenerator,
        IUAuthPasswordHasher hasher,
        ITokenHasher tokenHasher,
        ILoginIdentifierResolver identifierResolver,
        IOptions<UAuthServerOptions> options,
        IClock clock)
    {
        _accessOrchestrator = accessOrchestrator;
        _credentials = credentials;
        _authenticationSecurityManager = authenticationSecurityManager;
        _tokenGenerator = tokenGenerator;
        _numericCodeGenerator = numericCodeGenerator;
        _hasher = hasher;
        _tokenHasher = tokenHasher;
        _identifierResolver = identifierResolver;
        _options = options.Value;
        _clock = clock;
    }

    public async Task<GetCredentialsResult> GetAllAsync(AccessContext context, CancellationToken ct = default)
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
                    ExpiresAt = c.Security.ExpiresAt,
                    RevokedAt = c.Security.RevokedAt,
                    LastUsedAt = c.Metadata.LastUsedAt,
                    Source = c.Metadata.Source,
                    Version = c.Version,
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

            var credential = PasswordCredential.Create(
                id: null,
                tenant: context.ResourceTenant,
                userKey: subjectUser,
                secretHash: hash,
                security: CredentialSecurityState.Active(),
                metadata: new CredentialMetadata(),
                now: now);

            await _credentials.AddAsync(context.ResourceTenant, credential, innerCt);

            return AddCredentialResult.Success(credential.Id, credential.Type);
        });

        return await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }

    // TODO: Invalidate sessions or tokens associated with the credential when changing secret or revoking
    public async Task<ChangeCredentialResult> ChangeSecretAsync(AccessContext context, ChangeCredentialRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cmd = new AccessCommand<ChangeCredentialResult>(async innerCt =>
        {
            var subjectUser = context.GetTargetUserKey();
            var now = _clock.UtcNow;

            var credentials = await _credentials.GetByUserAsync(context.ResourceTenant, subjectUser, innerCt);
            var pwd = credentials.OfType<PasswordCredential>().Where(c => c.Security.IsUsable(now)).SingleOrDefault();

            if (pwd is null)
                throw new UAuthNotFoundException("credential_not_found");

            if (pwd.UserKey != subjectUser)
                throw new UAuthNotFoundException("credential_not_found");

            if (context.IsSelfAction)
            {
                if (string.IsNullOrWhiteSpace(request.CurrentSecret))
                    throw new UAuthNotFoundException("current_secret_required");

                if (!_hasher.Verify(pwd.SecretHash, request.CurrentSecret))
                    throw new UAuthConflictException("invalid_credentials");
            }

            if (_hasher.Verify(pwd.SecretHash, request.NewSecret))
                throw new UAuthValidationException("credential_secret_same");

            var oldVersion = pwd.Version;
            var newHash = _hasher.Hash(request.NewSecret);
            var updated = pwd.ChangeSecret(newHash, now);
            await _credentials.UpdateAsync(context.ResourceTenant, updated, oldVersion, innerCt);

            return ChangeCredentialResult.Success(pwd.Type);
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
            var updated = pwd.Revoke(now);
            await _credentials.UpdateAsync(context.ResourceTenant, updated, oldVersion, innerCt);

            return CredentialActionResult.Success();
        });

        return await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }

    public async Task<BeginCredentialResetResult> BeginResetAsync(AccessContext context, BeginCredentialResetRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cmd = new AccessCommand<BeginCredentialResetResult>(async innerCt =>
        {
            if (string.IsNullOrWhiteSpace(request.Identifier))
                throw new UAuthValidationException("identifier_required");

            var now = _clock.UtcNow;
            var validity = request.Validity ?? _options.ResetCredential.TokenValidity;

            var resolution = await _identifierResolver.ResolveAsync(context.ResourceTenant, request.Identifier, innerCt);

            if (resolution?.UserKey is not UserKey userKey)
            {
                return new BeginCredentialResetResult
                {
                    Token = null,
                    ExpiresAt = now.Add(validity)
                };
            }

            var state = await _authenticationSecurityManager
                .GetOrCreateFactorAsync(context.ResourceTenant, userKey, request.CredentialType, innerCt);

            string token;

            if (request.ResetCodeType == ResetCodeType.Token)
            {
                token = _tokenGenerator.Generate();
            }
            else if (request.ResetCodeType == ResetCodeType.Code)
            {
                token = _numericCodeGenerator.Generate(_options.ResetCredential.CodeLength);
            }
            else
            {
                throw new UAuthValidationException("invalid_reset_code_type");
            }

            var tokenHash = _tokenHasher.Hash(token);

            var updatedState = state.BeginReset(tokenHash, now, validity);
            await _authenticationSecurityManager.UpdateAsync(updatedState, state.SecurityVersion, innerCt);

            return new BeginCredentialResetResult
            {
                Token = token,
                ExpiresAt = now.Add(validity)
            };
        });

        return await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }

    public async Task<CredentialActionResult> CompleteResetAsync(AccessContext context, CompleteCredentialResetRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cmd = new AccessCommand<CredentialActionResult>(async innerCt =>
        {
            if (string.IsNullOrWhiteSpace(request.Identifier))
                throw new UAuthValidationException("identifier_required");

            if (string.IsNullOrWhiteSpace(request.ResetToken))
                throw new UAuthValidationException("reset_token_required");

            if (string.IsNullOrWhiteSpace(request.NewSecret))
                throw new UAuthValidationException("new_secret_required");

            var now = _clock.UtcNow;

            var resolution = await _identifierResolver.ResolveAsync(context.ResourceTenant, request.Identifier, innerCt);

            if (resolution?.UserKey is not UserKey userKey)
            {
                // Enumeration protection
                return CredentialActionResult.Success();
            }

            var state = await _authenticationSecurityManager
                .GetOrCreateFactorAsync(context.ResourceTenant, userKey, request.CredentialType, innerCt);

            if (!state.HasActiveReset(now))
                throw new UAuthConflictException("reset_request_not_active");

            if (state.IsResetExpired(now))
            {
                var version2 = state.SecurityVersion;
                var cleared = state.ClearReset();
                await _authenticationSecurityManager.UpdateAsync(cleared, version2, innerCt);
                throw new UAuthConflictException("reset_expired");
            }

            if (!_tokenHasher.Verify(state.ResetTokenHash!, request.ResetToken))
            {
                var version = state.SecurityVersion;
                var failed = state.RegisterResetFailure(now, _options.ResetCredential.MaxAttempts);
                await _authenticationSecurityManager.UpdateAsync(failed, version, innerCt);
                throw new UAuthConflictException("invalid_reset_token");
            }

            var credentials = await _credentials.GetByUserAsync(context.ResourceTenant, userKey, innerCt);
            var pwd = credentials.OfType<PasswordCredential>().FirstOrDefault(c => c.Security.IsUsable(now));

            if (pwd is null)
                throw new UAuthNotFoundException("credential_not_found");

            if (_hasher.Verify(pwd.SecretHash, request.NewSecret))
                throw new UAuthValidationException("credential_secret_same");

            var version3 = state.SecurityVersion;
            state = state.ConsumeReset(now);
            await _authenticationSecurityManager.UpdateAsync(state, version3, innerCt);

            var oldVersion = pwd.Version;
            var newHash = _hasher.Hash(request.NewSecret);
            var updated = pwd.ChangeSecret(newHash, now);

            await _credentials.UpdateAsync(context.ResourceTenant, updated, oldVersion, innerCt);

            return CredentialActionResult.Success();
        });

        return await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }

    public async Task<CredentialActionResult> CancelResetAsync(AccessContext context, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cmd = new AccessCommand<CredentialActionResult>(async innerCt =>
        {
            var userKey = context.GetTargetUserKey();

            var state = await _authenticationSecurityManager
                .GetOrCreateFactorAsync(context.ResourceTenant, userKey, CredentialType.Password, innerCt);

            if (!state.HasActiveReset(_clock.UtcNow))
                return CredentialActionResult.Success();

            var updated = state.ClearReset();
            await _authenticationSecurityManager.UpdateAsync(updated, state.SecurityVersion, innerCt);

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
