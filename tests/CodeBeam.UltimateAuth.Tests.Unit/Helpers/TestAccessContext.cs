using CodeBeam.UltimateAuth.Core.Contracts;
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
            resource: "test",
            targetUserKey: null,
            resourceTenant: TenantKey.Single,
            action: action,
            attributes: EmptyAttributes.Instance
        );
    }
}
