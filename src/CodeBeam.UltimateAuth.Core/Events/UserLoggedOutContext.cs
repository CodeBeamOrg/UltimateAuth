using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Core.Events;

/// <summary>
/// Represents contextual data emitted when a user logs out of the system.
/// 
/// This event is triggered when a logout operation is executed — either by explicit user action, automatic revocation, 
/// administrative force-logout, or tenant-level security policies.
/// 
/// Unlike session revoke which targets a specific session, this event reflects a higher-level “user has logged out” state and may
/// represent logout from a single session or all sessions depending on the workflow.
///
/// Typical use cases include:
/// - audit logging of logout activities
/// - updating user presence or activity services
/// - triggering notifications (e.g., “You have logged out from device X”)
/// - integrating with analytics or SIEM systems
/// </summary>
public sealed class UserLoggedOutContext : IAuthEventContext
{
    public TenantKey Tenant { get; }
    public UserKey UserKey { get; }
    public DateTimeOffset LoggedOutAt { get; }

    public AuthSessionId? SessionId { get; }
    public LogoutReason Reason { get; }

    public UserLoggedOutContext(
        TenantKey tenant,
        UserKey userKey,
        DateTimeOffset at,
        LogoutReason reason,
        AuthSessionId? sessionId = null)
    {
        Tenant = tenant;
        UserKey = userKey;
        LoggedOutAt = at;
        Reason = reason;
        SessionId = sessionId;
    }
}
