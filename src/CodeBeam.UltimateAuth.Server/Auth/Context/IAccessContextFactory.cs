using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Server.Auth;

public interface IAccessContextFactory
{
    Task<AccessContext> CreateAsync(AuthFlowContext authFlow, string action, string resource, string? resourceId = null, IDictionary<string, object>? attributes = null, CancellationToken ct = default);
    Task<AccessContext> CreateForExplicitTenantResourceAsync(AuthFlowContext authFlow, string action, string resource, TenantKey resourceTenant, string? resourceId = null, IDictionary<string, object>? attributes = null, CancellationToken ct = default);

    /// <summary>
    /// Creates an access context for an operation targeting multiple users.
    /// </summary>
    Task<AccessContext> CreateForUserTargetsAsync(AuthFlowContext authFlow, string action, string resource, IReadOnlyCollection<UserKey> targetUserKeys, CancellationToken ct = default);
}
