using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

// TenantId parameter only come from AuthFlowContext.
namespace CodeBeam.UltimateAuth.Server.Services;

/// <summary>
/// Read-only session query API.
/// Used for validation, UI, monitoring, and diagnostics.
/// </summary>
public interface ISessionQueryService
{
    /// <summary>
    /// Validates a session for runtime authentication.
    /// Hot path – must be fast and side-effect free.
    /// </summary>
    Task<SessionValidationResult> ValidateSessionAsync(SessionValidationContext context, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a specific session by id.
    /// </summary>
    Task<UAuthSession?> GetSessionAsync(AuthSessionId sessionId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all sessions belonging to a specific chain.
    /// </summary>
    Task<IReadOnlyList<UAuthSession>> GetSessionsByChainAsync(SessionChainId chainId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all session chains for a user.
    /// </summary>
    Task<IReadOnlyList<UAuthSessionChain>> GetChainsByUserAsync(UserKey userKey, CancellationToken ct = default);

    /// <summary>
    /// Resolves the chain id for a given session.
    /// </summary>
    Task<SessionChainId?> ResolveChainIdAsync(AuthSessionId sessionId, CancellationToken ct = default);
}
