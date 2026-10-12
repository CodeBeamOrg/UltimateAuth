using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
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
    private readonly IPasswordCredentialStoreFactory _credentialsFactory;
    private readonly IAuthenticationSecurityManager _authenticationSecurityManager;
    private readonly IOpaqueTokenGenerator _tokenGenerator;
    private readonly INumericCodeGenerator _numericCodeGenerator;
    private readonly IUAuthPasswordHasher _hasher;
    private readonly ITokenHasher _tokenHasher;
    private readonly ILoginIdentifierResolver _identifierResolver;
    private readonly ISessionStoreFactory _sessionFactory;
    private readonly ICredentialResetNotifier _resetNotifier;
    private readonly ICredentialSessionPolicy _credentialSessionPolicy;
    private readonly UAuthServerOptions _options;
    private readonly IClock _clock;

    public CredentialManagementService(
        IAccessOrchestrator accessOrchestrator,
        IPasswordCredentialStoreFactory credentialsFactory,
        IAuthenticationSecurityManager authenticationSecurityManager,
        IOpaqueTokenGenerator tokenGenerator,
        INumericCodeGenerator numericCodeGenerator,
        IUAuthPasswordHasher hasher,
        ITokenHasher tokenHasher,
        ILoginIdentifierResolver identifierResolver,
        ISessionStoreFactory sessionFactory,
        ICredentialResetNotifier resetNotifier,
        ICredentialSessionPolicy credentialSessionPolicy,
        IOptions<UAuthServerOptions> options,
        IClock clock)
    {
        _accessOrchestrator = accessOrchestrator;
        _credentialsFactory = credentialsFactory;
        _authenticationSecurityManager = authenticationSecurityManager;
        _tokenGenerator = tokenGenerator;
        _numericCodeGenerator = numericCodeGenerator;
        _hasher = hasher;
        _tokenHasher = tokenHasher;
        _identifierResolver = identifierResolver;
        _sessionFactory = sessionFactory;
        _resetNotifier = resetNotifier;
        _credentialSessionPolicy = credentialSessionPolicy;
        _options = options.Value;
        _clock = clock;
    }

    public async Task<CredentialsGetResult> GetAllAsync(AccessContext context, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cmd = new AccessCommand<CredentialsGetResult>(async innerCt =>
        {
            var subjectUser = context.GetTargetUserKey();
            var now = _clock.UtcNow;
            var store = _credentialsFactory.Create(context.ResourceTenant);
            var credentials = await store.GetByUserAsync(subjectUser, innerCt);

            var dtos = credentials
                .Select(c => new CredentialInfo
                {
                    Id = c.Id,
                    Type = c.Type,
                    Status = c.Security.Status(now),
                    CreatedAt = c.CreatedAt,
                    ExpiresAt = c.Security.ExpiresAt,
                    RevokedAt = c.Security.RevokedAt,
                    LastUsedAt = c.Metadata.LastUsedAt,
                    Source = c.Metadata.Source,
                    Version = c.Version,
                })
                .ToArray();

            return new CredentialsGetResult { Credentials = dtos };
        });

        return await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }

    public async Task<CredentialAddResult> AddAsync(AccessContext context, AddCredentialRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cmd = new AccessCommand<CredentialAddResult>(async innerCt =>
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

            var store = _credentialsFactory.Create(context.ResourceTenant);
            await store.AddAsync(credential, innerCt);

            return CredentialAddResult.Success(credential.Id, credential.Type);
        });

        return await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }

    public async Task<CredentialChangeResult> ChangeSecretAsync(AccessContext context, ChangeCredentialRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cmd = new AccessCommand<CredentialChangeResult>(async innerCt =>
        {
            var subjectUser = context.GetTargetUserKey();
            var now = _clock.UtcNow;

            var store = _credentialsFactory.Create(context.ResourceTenant);
            var credentials = await store.GetByUserAsync(subjectUser, innerCt);
            var pwd = credentials.OfType<PasswordCredential>().Where(c => c.Security.IsUsable(now)).FirstOrDefault();

            if (pwd is null)
                throw new UAuthNotFoundException("credential_not_found");

            if (pwd.UserKey != subjectUser)
                throw new UAuthNotFoundException("credential_not_found");

            if (string.IsNullOrWhiteSpace(request.NewSecret))
                throw new UAuthValidationException("new_secret_required");

            if (context.IsSelfAction)
            {
                if (string.IsNullOrWhiteSpace(request.CurrentSecret))
                    throw new UAuthValidationException("current_secret_required");

                if (!_hasher.Verify(pwd.SecretHash, request.CurrentSecret))
                    throw new UAuthConflictException("invalid_credentials");
            }

            if (_hasher.Verify(pwd.SecretHash, request.NewSecret))
                throw new UAuthValidationException("credential_secret_same");

            var oldVersion = pwd.Version;
            var newHash = _hasher.Hash(request.NewSecret);
            var updated = pwd.ChangeSecret(newHash, now);
            await store.SaveAsync(updated, oldVersion, innerCt);

            await ApplyCredentialSessionPolicyAsync(context.ResourceTenant, subjectUser, context.IsSelfAction 
                    ? CredentialChangeReason.SelfChange
                    : CredentialChangeReason.AdminChange,
                context.ActorChainId, now, innerCt);

            return CredentialChangeResult.Success(pwd.Type);
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

            var store = _credentialsFactory.Create(context.ResourceTenant);
            var credential = await store.GetAsync(new CredentialKey(context.ResourceTenant, request.Id), innerCt);

            if (credential is not PasswordCredential pwd)
                return CredentialActionResult.Fail("credential_not_found");

            if (pwd.UserKey != subjectUser)
                return CredentialActionResult.Fail("credential_not_found");

            var oldVersion = pwd.Version;
            var updated = pwd.Revoke(now);
            await store.SaveAsync(updated, oldVersion, innerCt);

            return CredentialActionResult.Success();
        });

        return await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }

    public async Task<CredentialBeginResetResult> BeginResetAsync(AccessContext context, BeginResetCredentialRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cmd = new AccessCommand<CredentialBeginResetResult>(async innerCt =>
        {

            if (_resetNotifier is NotConfiguredCredentialResetNotifier)
                throw NotConfiguredCredentialResetNotifier.CreateException();

            if (string.IsNullOrWhiteSpace(request.Identifier))
                throw new UAuthValidationException("identifier_required");

            // TODO: Support other credential types in the future (e.g., Totp, Passkey, etc.)
            if (request.CredentialType != CredentialType.Password)
                throw new UAuthValidationException("credential_reset_type_not_supported");

            if (request.ResetCodeType is not (ResetCodeType.Token or ResetCodeType.Code))
                throw new UAuthValidationException("invalid_reset_code_type");

            var now = _clock.UtcNow;
            // Anonymous callers must not control the proof's lifetime.
            var validity = _options.ResetCredential.TokenValidity;
            var expiresAt = now.Add(validity);

            var result = new CredentialBeginResetResult
            {
                ExpiresAt = expiresAt
            };

            var resolution = await _identifierResolver.ResolveAsync(context.ResourceTenant, request.Identifier, innerCt);

            if (context.IsAdminContext && resolution?.UserKey != context.GetTargetUserKey())
                throw new UAuthValidationException("credential_target_mismatch");

            if (resolution?.UserKey is not UserKey userKey)
                return result; // Enumeration protection

            var state = await _authenticationSecurityManager.GetOrCreateFactorAsync(context.ResourceTenant, userKey, request.CredentialType, innerCt);

            // An existing active reset must not be observable
            // through the anonymous endpoint.
            if (state.HasActiveReset(now) && context.IsAnonymousContext)
                return result;

            var token = request.ResetCodeType switch
            {
                ResetCodeType.Token => _tokenGenerator.Generate(),

                ResetCodeType.Code => _numericCodeGenerator.Generate(
                    _options.ResetCredential.CodeLength),

                _ => throw new UAuthValidationException(
                    "invalid_reset_code_type")
            };

            var tokenHash = _tokenHasher.Hash(token);

            var updatedState = state.BeginReset(tokenHash, now, validity);
            await _authenticationSecurityManager.UpdateAsync(updatedState, state.SecurityVersion, innerCt);

            await _resetNotifier.NotifyAsync(
                new CredentialResetNotification(
                    Tenant: context.ResourceTenant,
                    UserKey: userKey,
                    CredentialType: request.CredentialType,
                    CodeType: request.ResetCodeType,
                    Token: token,
                    ExpiresAt: expiresAt),
                innerCt);

            return result;
        });

        return await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }

    public async Task<CredentialActionResult> CompleteResetAsync(AccessContext context, CompleteResetCredentialRequest request, CancellationToken ct = default)
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

            // TODO: Support other credential types in the future (e.g., Totp, Passkey, etc.)
            if (request.CredentialType != CredentialType.Password)
                throw new UAuthValidationException("credential_reset_type_not_supported");

            var now = _clock.UtcNow;

            var resolution = await _identifierResolver.ResolveAsync(context.ResourceTenant, request.Identifier, innerCt);

            if (context.IsAdminContext && resolution?.UserKey != context.GetTargetUserKey())
                throw new UAuthValidationException("credential_target_mismatch");

            if (resolution?.UserKey is not UserKey userKey)
            {
                // Enumeration protection
                return CredentialActionResult.Success();
            }

            var state = await _authenticationSecurityManager.GetOrCreateFactorAsync(context.ResourceTenant, userKey, request.CredentialType, innerCt);

            if (state.IsResetExpired(now))
            {
                var version2 = state.SecurityVersion;
                var cleared = state.ClearReset();
                await _authenticationSecurityManager.UpdateAsync(cleared, version2, innerCt);

                if (context.IsAnonymousContext)
                    return CredentialActionResult.Success();

                throw new UAuthConflictException("reset_expired");
            }

            if (!state.HasActiveReset(now))
            {
                if (context.IsAnonymousContext)
                    return CredentialActionResult.Success();

                throw new UAuthConflictException("reset_request_not_active");
            }

            if (!_tokenHasher.Verify(state.ResetTokenHash!, request.ResetToken))
            {
                var version = state.SecurityVersion;
                var failed = state.RegisterResetFailure(now, _options.ResetCredential.MaxAttempts);
                await _authenticationSecurityManager.UpdateAsync(failed, version, innerCt);

                if (context.IsAnonymousContext)
                    return CredentialActionResult.Success();

                throw new UAuthConflictException("invalid_reset_token");
            }

            var store = _credentialsFactory.Create(context.ResourceTenant);
            var credentials = await store.GetByUserAsync(userKey, innerCt);
            var pwd = credentials.OfType<PasswordCredential>().FirstOrDefault(c => c.Security.IsUsable(now));

            if (pwd is null)
            {
                if (context.IsAnonymousContext)
                    return CredentialActionResult.Success();

                throw new UAuthNotFoundException("credential_not_found");
            }

            if (_hasher.Verify(pwd.SecretHash, request.NewSecret))
            {
                if (!context.IsAnonymousContext)
                    throw new UAuthValidationException("credential_secret_same");

                // Valid reset token, but password is unchanged.
                // Consume the token to preserve single-use semantics.
                var version = state.SecurityVersion;
                var consumed = state.ConsumeReset(now);

                await _authenticationSecurityManager.UpdateAsync(
                    consumed,
                    version,
                    innerCt);

                return CredentialActionResult.Success();
            }

            var version3 = state.SecurityVersion;
            state = state.ConsumeReset(now);
            await _authenticationSecurityManager.UpdateAsync(state, version3, innerCt);

            var oldVersion = pwd.Version;
            var newHash = _hasher.Hash(request.NewSecret);
            var updated = pwd.ChangeSecret(newHash, now);

            await store.SaveAsync(updated, oldVersion, innerCt);
            await ApplyCredentialSessionPolicyAsync(context.ResourceTenant ,userKey, CredentialChangeReason.Recovery, actorChainId: null, now, innerCt);

            return CredentialActionResult.Success();
        });

        return await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }

    // TODO: Implement cancel reset functionality if needed. For now, this method is commented out as it may not be required in the current implementation.
    //public async Task<CredentialActionResult> CancelResetAsync(AccessContext context, CancellationToken ct = default)
    //{
    //    ct.ThrowIfCancellationRequested();

    //    var cmd = new AccessCommand<CredentialActionResult>(async innerCt =>
    //    {
    //        var userKey = context.GetTargetUserKey();

    //        var state = await _authenticationSecurityManager
    //            .GetOrCreateFactorAsync(context.ResourceTenant, userKey, CredentialType.Password, innerCt);

    //        if (!state.HasActiveReset(_clock.UtcNow))
    //            return CredentialActionResult.Success();

    //        var updated = state.ClearReset();
    //        await _authenticationSecurityManager.UpdateAsync(updated, state.SecurityVersion, innerCt);

    //        return CredentialActionResult.Success();
    //    });

    //    return await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    //}

    public async Task<CredentialActionResult> DeleteAsync(AccessContext context, DeleteCredentialRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cmd = new AccessCommand<CredentialActionResult>(async innerCt =>
        {
            var subjectUser = context.GetTargetUserKey();
            var now = _clock.UtcNow;

            var store = _credentialsFactory.Create(context.ResourceTenant);
            var credential = await store.GetAsync(new CredentialKey(context.ResourceTenant, request.Id), innerCt);

            if (credential is not PasswordCredential pwd)
                return CredentialActionResult.Fail("credential_not_found");

            if (pwd.UserKey != subjectUser)
                return CredentialActionResult.Fail("credential_not_found");

            var oldVersion = pwd.Version;
            await store.DeleteAsync(new CredentialKey(context.ResourceTenant, pwd.Id), oldVersion, request.Mode, now,  innerCt);

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

        var store = _credentialsFactory.Create(tenant);
        await store.DeleteByUserAsync(userKey, DeleteMode.Soft, _clock.UtcNow, ct);
        return CredentialActionResult.Success();
    }

    // ----------------------------------------
    // HELPERS
    // ----------------------------------------
    private async Task ApplyCredentialSessionPolicyAsync(TenantKey tenant, UserKey subjectUser, CredentialChangeReason reason,
                       SessionChainId? actorChainId, DateTimeOffset now, CancellationToken ct)
    {
        var behavior = _credentialSessionPolicy.Resolve(reason);

        if (behavior == CredentialSessionBehavior.PreserveAll)
            return;

        var sessionStore = _sessionFactory.Create(tenant);

        switch (behavior)
        {
            case CredentialSessionBehavior.PreserveCurrent
                when reason == CredentialChangeReason.SelfChange && actorChainId is SessionChainId chainId:
                await sessionStore.RevokeOtherChainsAsync(subjectUser, chainId, now, ct);
                break;

            case CredentialSessionBehavior.PreserveCurrent:
            case CredentialSessionBehavior.RevokeAll:

                await sessionStore.RevokeAllChainsAsync(subjectUser, now, ct);
                break;

            default:
                throw new InvalidOperationException($"Unsupported credential session behavior: {behavior}");
        }
    }
}
