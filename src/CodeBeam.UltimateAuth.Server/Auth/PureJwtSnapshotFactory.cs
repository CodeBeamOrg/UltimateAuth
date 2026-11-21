using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Extensions;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using System.Globalization;
using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Server.Auth;

internal static class PureJwtSnapshotFactory
{
    public static AuthStateSnapshot Create(ClaimsPrincipal principal, UserKey userKey, TenantKey tenant)
    {
        var authenticatedAt = ParseAuthenticatedAt(principal.FindFirst(UAuthJwtProfileClaims.AuthenticatedAt)?.Value);

        return new AuthStateSnapshot
        {
            Identity = new AuthIdentitySnapshot
            {
                UserKey = userKey,
                Tenant = tenant,

                PrimaryUserName = principal.FindFirst(UAuthJwtProfileClaims.PrimaryUserName)?.Value,
                DisplayName = principal.FindFirst(UAuthJwtProfileClaims.DisplayName)?.Value,
                TimeZone = principal.FindFirst(UAuthJwtProfileClaims.TimeZone)?.Value,
                AuthenticatedAt = authenticatedAt,

                // PureJwt does not have a persisted session.
                SessionState = null,

                // JWT authentication does not establish the current persisted lifecycle state.
                UserStatus = UserStatus.Unknown
            },

            Claims = principal.ToClaimsSnapshot()
        };
    }

    private static DateTimeOffset? ParseAuthenticatedAt(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return DateTimeOffset.TryParseExact(value, "O", CultureInfo.InvariantCulture, DateTimeStyles.None, out var result)
            ? result
            : null;
    }
}
