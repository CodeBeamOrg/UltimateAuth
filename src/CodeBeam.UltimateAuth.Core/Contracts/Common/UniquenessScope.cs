namespace CodeBeam.UltimateAuth.Core.Contracts;

// TODO: Add global scope
/// <summary>
/// Defines the scope of uniqueness for an identifier.
/// </summary>
public enum UniquenessScope
{
    /// <summary>
    /// No uniqueness is enforced.
    /// Note that users still can't create duplicate identifiers.
    /// </summary>
    None = 0,

    /// <summary>
    /// Uniqueness is enforced within a single user.
    /// </summary>
    WithinUser = 10,

    /// <summary>
    /// Uniqueness is enforced within a tenant.
    /// </summary>
    Tenant = 20
}
