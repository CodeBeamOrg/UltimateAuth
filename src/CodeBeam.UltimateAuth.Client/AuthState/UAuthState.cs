using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Extensions;
using CodeBeam.UltimateAuth.Users.Contracts;
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

    public AuthIdentitySnapshot? Identity { get; private set; }
    public ClaimsSnapshot Claims { get; private set; } = ClaimsSnapshot.Empty;

    public DateTimeOffset? LastValidatedAt { get; private set; }

    /// <summary>
    /// Indicates whether the snapshot may be stale (e.g. after navigation, reload, or time-based heuristics).
    /// </summary>
    public bool IsStale { get; private set; }


    public event Action<UAuthStateChangeReason>? Changed;

    public bool IsAuthenticated => Identity is not null;

    public static UAuthState Anonymous() => new();

    internal void ApplySnapshot(AuthStateSnapshot snapshot, DateTimeOffset validatedAt)
    {
        Identity = snapshot.Identity;
        Claims = snapshot.Claims;

        IsStale = false;
        LastValidatedAt = validatedAt;

        Changed?.Invoke(UAuthStateChangeReason.Authenticated);
    }

    // TODO: Improve patch semantics with identifier, profile add, update or delete.
    internal void UpdateProfile(UpdateProfileRequest req)
    {
        if (Identity is null)
            return;

        Identity = Identity with
        {
            DisplayName = req.DisplayName ?? Identity.DisplayName
        };

        Changed?.Invoke(UAuthStateChangeReason.Patched);
    }

    internal void UpdateUserStatus(ChangeUserStatusSelfRequest req)
    {
        if (Identity is null)
            return;

        Identity = Identity with
        {
            UserStatus = UserStatusMapper.ToUserStatus(req.NewStatus)
        };

        Changed?.Invoke(UAuthStateChangeReason.Patched);
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
        Identity = null;
        Claims = ClaimsSnapshot.Empty;

        IsStale = false;

        Changed?.Invoke(UAuthStateChangeReason.Cleared);
    }

    public bool IsInRole(string role) => IsAuthenticated && Claims.IsInRole(role);

    public bool HasPermission(string permission) => IsAuthenticated && Claims.HasPermission(permission);

    public bool HasClaim(string type, string value) => IsAuthenticated && Claims.HasValue(type, value);

    public string? GetClaim(string type) => IsAuthenticated ? Claims.Get(type) : null;

    /// <summary>
    /// Creates a ClaimsPrincipal view for ASP.NET / Blazor integration.
    /// </summary>
    public ClaimsPrincipal ToClaimsPrincipal(string authenticationType = "UltimateAuth")
    {
        if (!IsAuthenticated || Identity is null)
            return new ClaimsPrincipal(new ClaimsIdentity());

        var claims = Claims.ToClaims().ToList();
        claims.Add(new Claim(ClaimTypes.NameIdentifier, Identity.UserKey.Value));

        if (!string.IsNullOrWhiteSpace(Identity.PrimaryUserName))
            claims.Add(new Claim(ClaimTypes.Name, Identity.PrimaryUserName));

        var identity = new ClaimsIdentity(claims, authenticationType, ClaimTypes.Name, ClaimTypes.Role);
        return new ClaimsPrincipal(identity);
    }
}
