using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Core.Events;

/// <summary>
/// Represents contextual data emitted when a user successfully completes the login process.
/// 
/// This event is triggered after the authentication workflow validates credentials
/// (or external identity provider assertions) and before or after the session creation step,
/// depending on pipeline configuration.
/// 
/// Typical use cases include:
/// - auditing successful logins
/// - triggering login notifications
/// - updating user activity dashboards
/// - integrating with SIEM or monitoring systems
/// 
/// NOTE:
/// This event is distinct from session create.
/// A user may log in without creating a new session (e.g., external SSO),
/// or multiple sessions may be created after a single login depending on client application flows.
/// </summary>
public sealed class UserLoggedInContext : IAuthEventContext
{
    public TenantKey Tenant { get; }
    public UserKey UserKey { get; }
    public DateTimeOffset LoggedInAt { get; }

    public DeviceContext? Device { get; }
    public AuthSessionId? SessionId { get; }

    public UserLoggedInContext(
        TenantKey tenant,
        UserKey userKey,
        DateTimeOffset at,
        DeviceContext? device = null,
        AuthSessionId? sessionId = null)
    {
        Tenant = tenant;
        UserKey = userKey;
        LoggedInAt = at;
        Device = device;
        SessionId = sessionId;
    }
}
