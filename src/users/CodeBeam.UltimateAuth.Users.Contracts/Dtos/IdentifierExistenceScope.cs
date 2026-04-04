namespace CodeBeam.UltimateAuth.Users.Contracts;

public enum IdentifierExistenceScope
{
    /// <summary>
    /// Checks only within the same user.
    /// </summary>
    WithinUser,

    /// <summary>
    /// Checks within tenant but only primary identifiers.
    /// </summary>
    TenantPrimaryOnly,

    /// <summary>
    /// Checks within tenant regardless of primary flag.
    /// </summary>
    TenantAny
}
