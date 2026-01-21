using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Server.Auth
{
    public interface IAccessContextFactory
    {
        AccessContext Create(AuthFlowContext authFlow, string action, string resource, string? resourceId = null, string? resourceTenantId = null, IReadOnlyDictionary<string, object>? attributes = null);
    }
}
