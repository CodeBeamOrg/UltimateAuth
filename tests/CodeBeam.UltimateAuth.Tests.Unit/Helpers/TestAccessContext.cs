using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Tests.Unit.Helpers;

internal static class TestAccessContext
{
    public static AccessContext WithAction(string action)
    {
        return new AccessContext(
            actorUserKey: null,
            actorTenant: TenantKey.Single,
            isAuthenticated: false,
            isSystemActor: false,
            actorChainId: null,
            resource: "test",
            targetUserKey: null,
            resourceTenant: TenantKey.Single,
            action: action,
            attributes: EmptyAttributes.Instance
        );
    }

    public static AccessContext ForUser(UserKey userKey, string action, TenantKey? tenant = null)
    {
        var t = tenant ?? TenantKey.Single;

        return new AccessContext(
            actorUserKey: userKey,
            actorTenant: t,
            isAuthenticated: true,
            isSystemActor: false,
            actorChainId: null,
            resource: "identifier",
            targetUserKey: userKey,
            resourceTenant: t,
            action: action,
            attributes: EmptyAttributes.Instance
        );
    }
}
