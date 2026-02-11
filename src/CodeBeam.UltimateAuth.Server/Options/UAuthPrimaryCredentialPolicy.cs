using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Options;

public sealed class UAuthPrimaryCredentialPolicy
{
    /// <summary>
    /// Default primary credential for UI-style requests.
    /// </summary>
    public PrimaryCredentialKind Ui { get; set; } = PrimaryCredentialKind.Stateful;

    /// <summary>
    /// Default primary credential for API requests.
    /// </summary>
    public PrimaryCredentialKind Api { get; set; } = PrimaryCredentialKind.Stateless;

    internal UAuthPrimaryCredentialPolicy Clone() => new()
    {
        Ui = Ui,
        Api = Api
    };
}
