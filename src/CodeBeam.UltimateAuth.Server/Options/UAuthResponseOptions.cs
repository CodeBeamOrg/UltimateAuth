namespace CodeBeam.UltimateAuth.Server.Options;

/// <summary>
/// Legacy authentication response configuration.
/// </summary>
[Obsolete(
    "UAuthResponseOptions is no longer supported. " +
    "Use UAuthGrantDeliveryOptions and UAuthRedirectOptions instead.")]
public sealed class UAuthResponseOptions
{
    [Obsolete("Use GrantDelivery.Session instead.")]
    public CredentialResponseOptions SessionIdDelivery { get; set; } = new();

    [Obsolete("Use GrantDelivery.AccessToken instead.")]
    public CredentialResponseOptions AccessTokenDelivery { get; set; } = new();

    [Obsolete("Use GrantDelivery.RefreshToken instead.")]
    public CredentialResponseOptions RefreshTokenDelivery { get; set; } = new();

    [Obsolete("Use Redirect.Login instead.")]
    public LoginRedirectOptions Login { get; set; } = new();

    [Obsolete("Use Redirect.Logout instead.")]
    public LogoutRedirectOptions Logout { get; set; } = new();
}
