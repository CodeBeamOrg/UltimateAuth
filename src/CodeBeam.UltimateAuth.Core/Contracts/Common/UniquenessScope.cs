namespace CodeBeam.UltimateAuth.Core.Contracts;

// TODO: Add global scope and implement other scopes properly and remove obsolete attribute.
/// <summary>
/// Defines the scope of uniqueness for an identifier.
/// </summary>
public enum UniquenessScope
{
    /// <summary>
    /// No uniqueness is enforced.
    /// Note that users still can't create duplicate identifiers.
    /// </summary>
    [Obsolete("None uniqueness is not currently supported. Use Tenant until the v0.5.0 uniqueness expansion.")]
    None = 0,

    /// <summary>
    /// Uniqueness is enforced within a single user.
    /// </summary>
    [Obsolete("WithinUser uniqueness is not currently supported. Use Tenant until the v0.5.0 uniqueness expansion.")]
    WithinUser = 10,

    /// <summary>
    /// Uniqueness is enforced within a tenant.
    /// </summary>
    Tenant = 20
}
