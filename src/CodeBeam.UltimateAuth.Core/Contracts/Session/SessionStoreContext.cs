using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Core.Contracts;

/// <summary>
/// Context information required by the session store when
/// creating or rotating sessions.
/// </summary>
public sealed class SessionStoreContext
{
    /// <summary>
    /// The authenticated user identifier.
    /// </summary>
    public required UserKey UserKey { get; init; }

    /// <summary>
    /// The tenant identifier, if multi-tenancy is enabled.
    /// </summary>
    public TenantKey Tenant { get; init; }

    /// <summary>
    /// Optional chain identifier.
    /// If null, a new chain should be created.
    /// </summary>
    public SessionChainId? ChainId { get; init; }

    /// <summary>
    /// Indicates whether the session is metadata-only
    /// (used in SemiHybrid mode).
    /// </summary>
    public bool IsMetadataOnly { get; init; }

    /// <summary>
    /// The UTC timestamp when the session was issued.
    /// </summary>
    public DateTimeOffset IssuedAt { get; init; }

    /// <summary>
    /// Optional device or client identifier.
    /// </summary>
    public required DeviceContext Device { get; init; }
}
