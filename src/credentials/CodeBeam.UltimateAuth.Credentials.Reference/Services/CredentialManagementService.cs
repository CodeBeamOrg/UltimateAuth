using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Options;
using CodeBeam.UltimateAuth.Credentials.Policies;
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
    private readonly IUAuthAtomicExecutor _atomicExecutor;
    private readonly PasswordCredentialWriter _passwordWriter;
    private readonly IPasswordHistoryStoreFactory _historyStores;
    private readonly IPasswordPolicyContextProvider _passwordContextProvider;
    private readonly IEffectiveServerOptionsProvider _effectiveServerOptionsProvider;
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
        IUAuthAtomicExecutor atomicExecutor,
        PasswordCredentialWriter passwordWriter,
        IPasswordHistoryStoreFactory historyStores,
        IPasswordPolicyContextProvider passwordContextProvider,
        IEffectiveServerOptionsProvider effectiveOptionsProvider,
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
        _atomicExecutor = atomicExecutor;
        _passwordWriter = passwordWriter;
        _historyStores = historyStores;
        _passwordContextProvider = passwordContextProvider;
        _effectiveServerOptionsProvider = effectiveOptionsProvider;
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

        var cmd = new AccessCommand<CredentialAddResult>(
            innerCt => _atomicExecutor.ExecuteAsync(
                async atomicCt =>
                {
                    var subjectUser = context.GetTargetUserKey();
                    var passwordContext = await _passwordContextProvider.CreateAsync(context.ResourceTenant, subjectUser, request.Secret, atomicCt);

                    // TODO: This is not correct client profile. Resolve true client profile in the future.
                    var effectiveServer = _effectiveServerOptionsProvider.GetEffective(context.ResourceTenant, UAuthClientProfile.NotSpecified);
                    var passwordOptions = PasswordPolicyOptionsResolver.Resolve(effectiveServer.Options.Password);

                    var credential = await _passwordWriter.CreateAsync(
                        context.ResourceTenant,
                        subjectUser,
                        request.Secret,
                        passwordContext,
                        passwordOptions,
                        _clock.UtcNow,
                        atomicCt);

                    return CredentialAddResult.Success(credential.Id, credential.Type);
                },
                innerCt));

        return await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }

    public async Task<CredentialChangeResult> ChangeSecretAsync(AccessContext context, ChangeCredentialRequest request, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var cmd = new AccessCommand<CredentialChangeResult>(
            innerCt => _atomicExecutor.ExecuteAsync(async atomicCt =>
            {
                var subjectUser = context.GetTargetUserKey();
                var now = _clock.UtcNow;

                var store = _credentialsFactory.Create(context.ResourceTenant);
                var credentials = await store.GetByUserAsync(subjectUser, atomicCt);
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

                var passwordContext = await _passwordContextProvider.CreateAsync(context.ResourceTenant, subjectUser, request.NewSecret, atomicCt);
                var effectiveServer = _effectiveServerOptionsProvider.GetEffective(context.ResourceTenant, UAuthClientProfile.NotSpecified);
                var passwordOptions = PasswordPolicyOptionsResolver.Resolve(effectiveServer.Options.Password);
                await _passwordWriter.ChangeAsync(pwd, request.NewSecret, passwordContext, passwordOptions, now, atomicCt);

                await ApplyCredentialSessionPolicyAsync(context.ResourceTenant, subjectUser, context.IsSelfAction
                        ? CredentialChangeReason.SelfChange
                        : CredentialChangeReason.AdminChange,
                    context.ActorChainId, now, atomicCt);

                return CredentialChangeResult.Success(pwd.Type);
            }, innerCt));

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
            {
                throw new UAuthValidationException("credential_target_mismatch");
            }

            if (resolution?.UserKey is not UserKey userKey)
                return CredentialActionResult.Success();

            var state = await _authenticationSecurityManager
                .GetOrCreateFactorAsync(context.ResourceTenant, userKey, request.CredentialType, innerCt);

            if (state.IsResetExpired(now))
            {
                var cleared = state.ClearReset();

                await _authenticationSecurityManager.UpdateAsync(cleared, state.SecurityVersion, innerCt);

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
                var failed = state.RegisterResetFailure(now, _options.ResetCredential.MaxAttempts);

                await _authenticationSecurityManager.UpdateAsync(failed, state.SecurityVersion, innerCt);

                if (context.IsAnonymousContext)
                    return CredentialActionResult.Success();

                throw new UAuthConflictException("invalid_reset_token");
            }

            // A valid reset proof has now been established.
            // Password validation errors may be returned without
            // disclosing whether an unverified account exists.

            var store = _credentialsFactory.Create(context.ResourceTenant);

            var credentials = await store.GetByUserAsync(userKey, innerCt);

            var pwd = credentials.OfType<PasswordCredential>().FirstOrDefault(c => c.Security.IsUsable(now));

            if (pwd is null)
            {
                if (context.IsAnonymousContext)
                    return CredentialActionResult.Success();

                throw new UAuthNotFoundException("credential_not_found");
            }

            return await _atomicExecutor.ExecuteAsync(
                async atomicCt =>
                {
                    // Re-read security state within the atomic scope.
                    // This protects against consuming a stale proof
                    // after another reset operation.
                    var currentState = await _authenticationSecurityManager
                        .GetOrCreateFactorAsync(
                            context.ResourceTenant,
                            userKey,
                            request.CredentialType,
                            atomicCt);

                    if (!currentState.HasActiveReset(now) ||
                        currentState.IsResetExpired(now) ||
                        !_tokenHasher.Verify(currentState.ResetTokenHash!, request.ResetToken))
                    {
                        if (context.IsAnonymousContext)
                            return CredentialActionResult.Success();

                        throw new UAuthConflictException("reset_request_not_active");
                    }

                    // Re-read the credential inside the atomic scope.
                    var currentCredential = await store.GetAsync(
                        new CredentialKey(
                            context.ResourceTenant,
                            pwd.Id),
                        atomicCt);

                    if (currentCredential is not PasswordCredential currentPwd ||
                        currentPwd.UserKey != userKey ||
                        !currentPwd.Security.IsUsable(now))
                    {
                        if (context.IsAnonymousContext)
                            return CredentialActionResult.Success();

                        throw new UAuthNotFoundException("credential_not_found");
                    }

                    // Strength + current password + history validation.
                    // Validation failure throws before token consumption.
                    var passwordContext = await _passwordContextProvider.CreateAsync(context.ResourceTenant, userKey, request.NewSecret, atomicCt);
                    var effectiveServer = _effectiveServerOptionsProvider.GetEffective(context.ResourceTenant, UAuthClientProfile.NotSpecified);
                    var passwordOptions = PasswordPolicyOptionsResolver.Resolve(effectiveServer.Options.Password);

                    try
                    {
                        await _passwordWriter.ChangeAsync(
                            currentPwd,
                            request.NewSecret,
                            passwordContext,
                            passwordOptions,
                            now,
                            atomicCt);
                    }
                    catch (UAuthValidationException) when (context.IsAnonymousContext)
                    {
                        return CredentialActionResult.Success();
                    }

                    var consumed = currentState.ConsumeReset(now);

                    await _authenticationSecurityManager.UpdateAsync(consumed, currentState.SecurityVersion, atomicCt);

                    await ApplyCredentialSessionPolicyAsync(
                        context.ResourceTenant,
                        userKey,
                        CredentialChangeReason.Recovery,
                        actorChainId: null,
                        now,
                        atomicCt);

                    return CredentialActionResult.Success();
                },
                innerCt);
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

        var cmd = new AccessCommand<CredentialActionResult>(
            innerCt => _atomicExecutor.ExecuteAsync(
                async atomicCt =>
                {
                    var subjectUser = context.GetTargetUserKey();
                    var now = _clock.UtcNow;
                    var tenant = context.ResourceTenant;

                    var key = new CredentialKey(tenant, request.Id);

                    var store = _credentialsFactory.Create(tenant);

                    var credential = await store.GetAsync(key, atomicCt);

                    if (credential is not PasswordCredential pwd)
                        return CredentialActionResult.Fail("credential_not_found");

                    if (pwd.UserKey != subjectUser)
                        return CredentialActionResult.Fail("credential_not_found");

                    await store.DeleteAsync(key, pwd.Version, request.Mode, now, atomicCt);

                    if (request.Mode == DeleteMode.Hard)
                    {
                        var historyStore = _historyStores.Create(tenant);

                        await historyStore.DeleteByCredentialAsync(key, atomicCt);
                    }

                    return CredentialActionResult.Success();
                },
                innerCt));

        return await _accessOrchestrator.ExecuteAsync(context, cmd, ct);
    }

    // ----------------------------------------
    // INTERNAL ONLY - NEVER CALL THEM DIRECTLY
    // ----------------------------------------
    async Task<CredentialActionResult> IUserCredentialsInternalService.DeleteInternalAsync(TenantKey tenant, UserKey userKey, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        await _atomicExecutor.ExecuteAsync(
            async atomicCt =>
            {
                var store = _credentialsFactory.Create(tenant);

                await store.DeleteByUserAsync(userKey, DeleteMode.Soft, _clock.UtcNow, atomicCt);
            },
            ct);

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

        await sessionStore.ExecuteAsync(async innerCt =>
        {
            switch (behavior)
            {
                case CredentialSessionBehavior.PreserveCurrent when reason == CredentialChangeReason.SelfChange && actorChainId is SessionChainId chainId:
                    await sessionStore.RevokeOtherChainsAsync(subjectUser, chainId, now, innerCt);
                    break;

                case CredentialSessionBehavior.PreserveCurrent:
                case CredentialSessionBehavior.RevokeAll:
                    await sessionStore.RevokeAllChainsAsync(subjectUser, now, innerCt);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported credential session behavior: {behavior}");
            }
        }, ct);
    }
}
