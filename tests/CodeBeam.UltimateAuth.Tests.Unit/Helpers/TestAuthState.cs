using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Tests.Unit.Helpers;

public static class TestAuthState
{
    public static UAuthState Anonymous()
        => UAuthState.Anonymous();

    public static UAuthState Authenticated(
        string userId = "user-1",
        params (string Type, string Value)[] claims)
    {
        var state = UAuthState.Anonymous();

        var identity = new AuthIdentitySnapshot
        {
            UserKey = UserKey.FromString(userId),
            Tenant = TenantKeys.Single,
            SessionState = SessionState.Active,
            UserStatus = UserStatus.Active
        };

        var snapshot = new AuthStateSnapshot
        {
            Identity = identity,
            Claims = ClaimsSnapshot.From(claims)
        };

        state.ApplySnapshot(snapshot, DateTimeOffset.UtcNow);

        return state;
    }

    public static UAuthState WithRoles(params string[] roles)
    {
        return Authenticated(
            claims: roles.Select(r => (ClaimTypes.Role, r)).ToArray());
    }

    public static UAuthState WithPermissions(params string[] permissions)
    {
        return Authenticated(
            claims: permissions.Select(p => ("uauth:permission", p)).ToArray());
    }

    public static UAuthState WithSession(SessionState sessionState)
    {
        var state = Authenticated();

        var identity = state.Identity! with
        {
            SessionState = sessionState
        };

        var snapshot = new AuthStateSnapshot
        {
            Identity = identity,
            Claims = state.Claims
        };

        state.ApplySnapshot(snapshot, DateTimeOffset.UtcNow);

        return state;
    }

    public static UAuthState Full(
        string userId,
        string[] roles,
        string[] permissions)
    {
        var claims = new List<(string, string)>();

        claims.AddRange(roles.Select(r => (ClaimTypes.Role, r)));
        claims.AddRange(permissions.Select(p => ("uauth:permission", p)));

        return Authenticated(userId, claims.ToArray());
    }
}
