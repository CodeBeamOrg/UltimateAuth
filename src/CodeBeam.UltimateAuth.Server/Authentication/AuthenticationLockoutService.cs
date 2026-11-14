using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.Security;
using CodeBeam.UltimateAuth.Server.Authentication;
using CodeBeam.UltimateAuth.Server.Infrastructure;

namespace CodeBeam.UltimateAuth.Server.Security;

internal sealed class AuthenticationLockoutService(
    IAuthenticationSecurityManager securityManager,
    IUserRuntimeStateProvider users,
    IAccessOrchestrator accessOrchestrator)
    : IAuthenticationLockoutService
{
    public Task<AuthenticationLockoutInfo> GetAsync(AccessContext context, LockoutQueryRequest request, CancellationToken ct = default)
    {
        ValidateScope(request.Scope, request.CredentialType);

        var command = new AccessCommand<AuthenticationLockoutInfo>(
            async innerCt =>
            {
                var userKey = await RequireUserAsync(context, innerCt);

                var state = await GetStateAsync(context, userKey, request.Scope, request.CredentialType, innerCt);

                return ToInfo(state);
            });

        return accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public Task<AuthenticationUnlockResult> UnlockAsync(AccessContext context, UnlockAuthenticationRequest request, CancellationToken ct = default)
    {
        ValidateScope(request.Scope, request.CredentialType);

        var command = new AccessCommand<AuthenticationUnlockResult>(
            async innerCt =>
            {
                var userKey = await RequireUserAsync(context, innerCt);

                AuthenticationSecurityState after;

                if (request.Scope == AuthenticationSecurityScope.Account)
                {
                    after = await securityManager.MutateAccountAsync(context.ResourceTenant, userKey, state => state.Unlock(), innerCt);
                }
                else
                {
                    after = await securityManager.MutateFactorAsync(context.ResourceTenant, userKey, request.CredentialType!.Value, state => state.Unlock(), innerCt);
                }

                return new AuthenticationUnlockResult(request.Scope, request.CredentialType, State: ToInfo(after));
            });

        return accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    private async Task<UserKey> RequireUserAsync(AccessContext context, CancellationToken ct)
    {
        var userKey = context.GetTargetUserKey();

        var user = await users.GetAsync(context.ResourceTenant, userKey, ct);

        if (user is null || !user.Exists || user.IsDeleted)
            throw new UAuthNotFoundException("user_not_found");

        return userKey;
    }

    private async Task<AuthenticationSecurityState> GetStateAsync(
        AccessContext context,
        UserKey userKey,
        AuthenticationSecurityScope scope,
        CredentialType? credentialType,
        CancellationToken ct)
    {
        if (scope == AuthenticationSecurityScope.Account)
        {
            return await securityManager.GetOrCreateAccountAsync(context.ResourceTenant, userKey, ct);
        }

        return await securityManager.GetOrCreateFactorAsync(context.ResourceTenant, userKey, credentialType!.Value, ct);
    }

    private static AuthenticationLockoutInfo ToInfo(AuthenticationSecurityState state)
    {
        return new AuthenticationLockoutInfo(
            state.Scope,
            state.CredentialType,
            state.IsLocked(DateTimeOffset.UtcNow),
            state.FailedAttempts,
            state.LastFailedAt,
            state.LockedUntil,
            state.RequiresReauthentication,
            state.SecurityVersion);
    }

    private static void ValidateScope(AuthenticationSecurityScope scope, CredentialType? credentialType)
    {
        if (scope == AuthenticationSecurityScope.Account && credentialType is null)
            return;

        if (scope == AuthenticationSecurityScope.Factor && credentialType is CredentialType type && Enum.IsDefined(type))
            return;

        throw new UAuthValidationException("invalid_authentication_lockout_scope");
    }
}
