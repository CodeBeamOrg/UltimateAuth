using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Extensions;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Client;

/// <summary>
/// Represents the client-side authentication snapshot for UltimateAuth.
///
/// This is a lightweight, memory-only view of the current authentication state.
/// It is not a security boundary and must always be validated server-side.
/// </summary>
public sealed class UAuthState
{
    private UAuthState() { }

    public bool IsAuthenticated { get; private set; }

    public UserKey? UserKey { get; private set; }

    public TenantKey Tenant { get; private set; }

    /// <summary>
    /// When this authentication snapshot was created.
    /// </summary>
    public DateTimeOffset? AuthenticatedAt { get; private set; }

    /// <summary>
    /// When this snapshot was last validated or refreshed.
    /// </summary>
    public DateTimeOffset? LastValidatedAt { get; private set; }

    /// <summary>
    /// Indicates whether the snapshot may be stale
    /// (e.g. after navigation, reload, or time-based heuristics).
    /// </summary>
    public bool IsStale { get; private set; }

    public ClaimsSnapshot Claims { get; private set; } = ClaimsSnapshot.Empty;

    public event Action<UAuthStateChangeReason>? Changed;

    public static UAuthState Anonymous() => new();

    internal void ApplySnapshot(AuthStateSnapshot snapshot, DateTimeOffset validatedAt)
    {
        UserKey = snapshot.Identity.UserKey;
        Tenant = snapshot.Identity.Tenant;
        Claims = snapshot.Claims;

        IsAuthenticated = true;

        AuthenticatedAt = snapshot.Identity.AuthenticatedAt;
        LastValidatedAt = validatedAt;
        IsStale = false;

        Changed?.Invoke(UAuthStateChangeReason.Authenticated);
    }

    internal void MarkValidated(DateTimeOffset now)
    {
        if (!IsAuthenticated)
            return;

        LastValidatedAt = now;
        IsStale = false;

        Changed?.Invoke(UAuthStateChangeReason.Validated);
    }

    internal void MarkStale()
    {
        if (!IsAuthenticated)
            return;

        IsStale = true;
        Changed?.Invoke(UAuthStateChangeReason.MarkedStale);
    }

    internal void Clear()
    {
        Claims = ClaimsSnapshot.Empty;

        UserKey = null;
        IsAuthenticated = false;

        AuthenticatedAt = null;
        LastValidatedAt = null;
        IsStale = false;

        Changed?.Invoke(UAuthStateChangeReason.Cleared);
    }

    /// <summary>
    /// Creates a ClaimsPrincipal view for ASP.NET / Blazor integration.
    /// </summary>
    public ClaimsPrincipal ToClaimsPrincipal(string authenticationType = "UltimateAuth")
    {
        if (!IsAuthenticated || UserKey is null)
            return new ClaimsPrincipal(new ClaimsIdentity());

        var claims = Claims.ToClaims().ToList();
        claims.Add(new Claim(ClaimTypes.NameIdentifier, UserKey.Value));

        if (!string.IsNullOrWhiteSpace(Claims.Get(ClaimTypes.Name)))
            claims.Add(new Claim(ClaimTypes.Name, Claims.Get(ClaimTypes.Name)!));

        var identity = new ClaimsIdentity(claims, authenticationType, ClaimTypes.Name, ClaimTypes.Role);
        return new ClaimsPrincipal(identity);
    }
}
