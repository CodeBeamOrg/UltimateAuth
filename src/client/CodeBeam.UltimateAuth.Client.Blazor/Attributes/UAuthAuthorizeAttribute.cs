namespace CodeBeam.UltimateAuth.Client.Blazor;

/// <summary>
/// Declares UltimateAuth authorization requirements for a Blazor component or page.
/// </summary>
/// <remarks>
/// <para>
/// The attribute can be used to associate role and permission requirements with routable or authorization-aware Blazor components.
/// </para>
/// <para>
/// Role and permission values are expressed as comma-separated lists. The effective
/// authorization behavior is determined by the UltimateAuth authorization pipeline that consumes this metadata.
/// </para>
/// <para>
/// This attribute describes authorization requirements only. It does not perform authorization by itself.
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class)]
public sealed class UAuthAuthorizeAttribute : Attribute
{
    /// <summary>
    /// Gets or sets the comma-separated roles associated with the authorization requirement.
    /// </summary>
    /// <remarks>
    /// A <see langword="null"/>, empty, or whitespace value indicates that no explicit role requirement is declared by this property.
    /// </remarks>
    public string? Roles { get; set; }

    /// <summary>
    /// Gets or sets the comma-separated UltimateAuth permissions associated with the authorization requirement.
    /// </summary>
    /// <remarks>
    /// A <see langword="null"/>, empty, or whitespace value indicates that no explicit 
    /// permission requirement is declared by this property.
    /// </remarks>
    public string? Permissions { get; set; }
}
