using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Core.Contracts;

/// <summary>
/// Lightweight session context resolved from the incoming request.
/// Does NOT load or validate the session.
/// Used only by middleware and engines as input.
/// </summary>
public sealed class SessionContext
{
    public AuthSessionId? SessionId { get; }
    public TenantKey? Tenant { get; }

    public bool IsAnonymous => SessionId is null;

    private SessionContext(AuthSessionId? sessionId, TenantKey? tenant)
    {
        SessionId = sessionId;
        Tenant = tenant;
    }

    public static SessionContext Anonymous() => new(null, null);

    public static SessionContext FromSessionId(AuthSessionId sessionId, TenantKey tenant) => new(sessionId, tenant);
}
