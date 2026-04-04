using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Core.Infrastructure;

public static class SessionValidationMapper
{
    public static SessionValidationResult ToDomain(SessionValidationInfo dto)
    {
        var state = (SessionState)dto.State;

        if (!dto.IsValid || dto.Snapshot.Identity is null)
        {
            return SessionValidationResult.Invalid(state);
        }

        var tenant = TenantKey.FromInternal(dto.Snapshot.Identity.Tenant);

        UserKey? userKey = string.IsNullOrWhiteSpace(dto.Snapshot.Identity.UserKey)
            ? null
            : UserKey.Parse(dto.Snapshot.Identity.UserKey, null);

        ClaimsSnapshot claims;

        if (dto.Snapshot.Claims is null)
        {
            claims = ClaimsSnapshot.Empty;
        }
        else
        {
            var builder = ClaimsSnapshot.Create();

            foreach (var (type, values) in dto.Snapshot.Claims.Claims)
            {
                builder.AddMany(type, values);
            }

            foreach (var role in dto.Snapshot.Claims.Roles)
            {
                builder.AddRole(role);
            }

            foreach (var permission in dto.Snapshot.Claims.Permissions)
            {
                builder.AddPermission(permission);
            }

            claims = builder.Build();
        }
        
        AuthSessionId.TryCreate("temp", out AuthSessionId tempSessionId);

        return SessionValidationResult.Active(
            tenant,
            userKey,
            tempSessionId,     // TODO: This is TEMP add real
            SessionChainId.New(),     // TEMP
            SessionRootId.New(),      // TEMP
            claims,
            dto.Snapshot.Identity.AuthenticatedAt ?? DateTimeOffset.UtcNow,
            null
        );
    }

    public static SessionSecurityContext? ToSecurityContext(SessionValidationResult result)
    {
        if (!result.IsValid)
        {
            if (result?.SessionId is null)
                return null;

            return new SessionSecurityContext
            {
                SessionId = result.SessionId.Value,
                State = result.State,
                ChainId = result.ChainId,
                UserKey = result.UserKey,
                BoundDeviceId = result.BoundDeviceId
            };
        }

        return new SessionSecurityContext
        {
            SessionId = result.SessionId!.Value,
            State = SessionState.Active,
            ChainId = result.ChainId,
            UserKey = result.UserKey,
            BoundDeviceId = result.BoundDeviceId
        };
    }
}
