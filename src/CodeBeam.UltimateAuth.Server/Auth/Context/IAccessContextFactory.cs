using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Server.Auth
{
    public interface IAccessContextFactory
    {
        Task<AccessContext> CreateAsync(AuthFlowContext authFlow, string action, string resource, string? resourceId = null, string? resourceTenantId = null, IDictionary<string, object>? attributes = null, CancellationToken ct = default);
    }
}
