using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Options;

public sealed class UAuthPrimaryCredentialPolicy
{
    /// <summary>
    /// Default primary credential for UI-style requests.
    /// </summary>
    public PrimaryTokenKind Ui { get; set; } = PrimaryTokenKind.Session;

    /// <summary>
    /// Default primary credential for API requests.
    /// </summary>
    public PrimaryTokenKind Api { get; set; } = PrimaryTokenKind.AccessToken;

    internal UAuthPrimaryCredentialPolicy Clone() => new()
    {
        Ui = Ui,
        Api = Api
    };
}
