using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Core.Contracts;

/// <summary>
/// Represents the context in which a session is issued
/// (login, refresh, reauthentication).
/// </summary>
public sealed class AuthenticatedSessionContext
{
    public TenantKey Tenant { get; init; }
    public required UserKey UserKey { get; init; }
    public required DeviceContext Device { get; init; }
    public DateTimeOffset Now { get; init; }
    public ClaimsSnapshot? Claims { get; init; }
    public required SessionMetadata Metadata { get; init; }
    public required UAuthMode Mode { get; init; }

    /// <summary>
    /// Optional chain identifier.
    /// If null, a new chain will be created.
    /// If provided, session will be issued under the existing chain.
    /// </summary>
    public SessionChainId? ChainId { get; init; }

    /// <summary>
    /// Indicates that authentication has already been completed.
    /// This context MUST NOT be constructed from raw credentials.
    /// </summary>
    public bool IsAuthenticated { get; init; } = true;
}
