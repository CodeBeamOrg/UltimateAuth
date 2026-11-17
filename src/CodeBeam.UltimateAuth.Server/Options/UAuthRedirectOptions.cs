namespace CodeBeam.UltimateAuth.Server.Options;

/// <summary>
/// Configures authentication flow redirects.
/// </summary>
public sealed class UAuthRedirectOptions
{
    /// <summary>
    /// Configures redirects after login and reauthentication.
    /// </summary>
    public LoginRedirectOptions Login { get; set; } = new();

    /// <summary>
    /// Configures redirects after logout.
    /// </summary>
    public LogoutRedirectOptions Logout { get; set; } = new();

    internal UAuthRedirectOptions Clone() => new()
    {
        Login = Login.Clone(),
        Logout = Logout.Clone()
    };
}
