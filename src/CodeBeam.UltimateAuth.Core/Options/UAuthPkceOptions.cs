namespace CodeBeam.UltimateAuth.Core.Options;

/// <summary>
/// Configuration settings for PKCE (Proof Key for Code Exchange)
/// authorization flows. Controls how long authorization codes remain
/// valid before they must be exchanged for tokens.
/// </summary>
public sealed class UAuthPkceOptions
{
    /// <summary>
    /// Lifetime of a PKCE authorization code in seconds.
    /// Shorter values provide stronger replay protection,
    /// while longer values allow more tolerance for slow clients.
    /// </summary>
    public int AuthorizationCodeLifetimeSeconds { get; set; } = 120;

    public int MaxVerificationAttempts { get; set; } = 5;

    internal UAuthPkceOptions Clone() => new()
    {
        AuthorizationCodeLifetimeSeconds = AuthorizationCodeLifetimeSeconds,
        MaxVerificationAttempts = MaxVerificationAttempts,
    };
}
