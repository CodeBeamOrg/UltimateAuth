using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Extensions;
using CodeBeam.UltimateAuth.Users.Contracts;
using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Client;

/// <summary>
/// Represents the client-side authentication snapshot for UltimateAuth.
/// <para>
/// This is a lightweight, memory-only view of the current authentication state.
/// It is not a security boundary and must always be validated server-side.
/// </para>
/// </summary>
public sealed class UAuthState
{
    private UAuthState() { }

    /// <summary>
    /// Gets the current authenticated identity snapshot, or null if the user is not authenticated.
    /// </summary>
    public AuthIdentitySnapshot? Identity { get; private set; }

    /// <summary>
    /// Gets the current claims snapshot for the authenticated user, or an empty snapshot if the user is not authenticated.
    /// </summary>
    public ClaimsSnapshot Claims { get; private set; } = ClaimsSnapshot.Empty;

    /// <summary>
    /// Gets the timestamp of the last successful validation of the authentication state, or null if it has never been validated.
    /// </summary>
    public DateTimeOffset? LastValidatedAt { get; private set; }

    /// <summary>
    /// Indicates whether the snapshot may be stale (e.g. after navigation, reload, or time-based heuristics).
    /// </summary>
    public bool IsStale { get; private set; }

    /// <summary>
    /// Occurs when the authentication state has changed, such as after login, logout, or profile updates.
    /// </summary>
    public event Action<UAuthStateChangeReason>? Changed;
    internal Action? RequestRender;

    /// <summary>
    /// Gets a value indicating whether the user is currently authenticated (i.e., has a valid identity).
    /// </summary>
    public bool IsAuthenticated => Identity is not null;

    /// <summary>
    /// Gets a value indicating whether the authentication state needs to be validated (i.e., the user is authenticated but the snapshot is stale).
    /// </summary>
    public bool NeedsValidation => IsAuthenticated && IsStale;

    /// <summary>
    /// Creates a new anonymous (unauthenticated) instance of <see cref="UAuthState"/>.
    /// </summary>
    public static UAuthState Anonymous() => new();

    internal void ApplySnapshot(AuthStateSnapshot snapshot, DateTimeOffset validatedAt)
    {
        Identity = snapshot.Identity;
        Claims = snapshot.Claims;

        _compiledPermissions = new CompiledPermissionSet(Claims.Permissions.Select(Permission.From));

        IsStale = false;
        LastValidatedAt = validatedAt;

        Changed?.Invoke(UAuthStateChangeReason.Authenticated);
    }

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

    /// <summary>
    /// Marks the authentication state as stale and requests a re-render of the UI.
    /// </summary>
    public void Touch(bool updateState = true)
    {
        if (updateState)
        {
            IsStale = true;
        }
        
        RequestRender?.Invoke();
    }

    internal void Clear()
    {
        Identity = null;
        Claims = ClaimsSnapshot.Empty;

        IsStale = false;

        Changed?.Invoke(UAuthStateChangeReason.Cleared);
    }

    /// <summary>
    /// Determines whether the current authenticated user is in the specified role.
    /// </summary>
    /// <param name="role"></param>
    /// <returns></returns>
    public bool IsInRole(string role) => IsAuthenticated && Claims.IsInRole(role);

    private CompiledPermissionSet? _compiledPermissions;

    /// <summary>
    /// Determines whether the current authenticated user has the specified permission.
    /// </summary>
    /// <param name="permission"></param>
    /// <returns></returns>
    public bool HasPermission(string permission)
    {
        if (!IsAuthenticated)
            return false;

        if (Claims.HasPermission(permission))
            return true;

        return _compiledPermissions?.IsAllowed(permission) == true;
    }

    /// <summary>
    /// Determines whether the current authenticated user has any of the specified permissions.
    /// </summary>
    /// <param name="permissions"></param>
    /// <returns></returns>
    public bool HasAnyPermission(params string[] permissions)
    {
        foreach (var perm in permissions)
        {
            if (HasPermission(perm))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Determines whether the current authenticated user has the specified claim type and value.
    /// </summary>
    public bool HasClaim(string type, string value) => IsAuthenticated && Claims.HasValue(type, value);

    /// <summary>
    /// Gets the value of the specified claim type for the current authenticated user, or null if the claim does not exist or the user is not authenticated.
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public string? GetClaim(string type) => IsAuthenticated ? Claims.Get(type) : null;

    /// <summary>
    /// Creates a ClaimsPrincipal view for ASP.NET / Blazor integration.
    /// </summary>
    public ClaimsPrincipal ToClaimsPrincipal(string authenticationType = UAuthConstants.SchemeDefaults.GlobalScheme)
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
