namespace CodeBeam.UltimateAuth.Authorization.Contracts;

public sealed record UserPermissionsResult
{
    /// <summary>
    /// Gets the normalized, role-derived permissions assigned to the user.
    /// These permissions do not represent final authorization decisions.
    /// </summary>
    public required IReadOnlyList<Permission> Permissions { get; init; }
}
