using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Users;

/// <summary>
/// Optional integration point for reacting to user lifecycle events.
/// Implemented by plugin domains (Credentials, Authorization, Audit, etc).
/// </summary>
public interface IUserLifecycleIntegration
{
    Task OnUserCreatedAsync(TenantKey tenant, UserKey userKey, object request, UAuthClientProfile clientProfile, CancellationToken ct = default);

    Task OnUserDeletedAsync(TenantKey tenant, UserKey userKey, DeleteMode mode, CancellationToken ct = default);
}
