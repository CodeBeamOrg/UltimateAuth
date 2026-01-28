using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Users.Abstractions;

/// <summary>
/// Optional integration point for reacting to user lifecycle events.
/// Implemented by plugin domains (Credentials, Authorization, Audit, etc).
/// </summary>
public interface IUserLifecycleIntegration
{
    Task OnUserCreatedAsync(string? tenantId, UserKey userKey, object request, CancellationToken ct = default);

    Task OnUserDeletedAsync(string? tenantId, UserKey userKey, DeleteMode mode, CancellationToken ct = default);
}
