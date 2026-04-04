using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Options;

public sealed class UAuthPrimaryCredentialPolicy
{
    /// <summary>
    /// Default primary credential for UI-style requests.
    /// </summary>
    public PrimaryGrantKind Ui { get; set; } = PrimaryGrantKind.Stateful;

    /// <summary>
    /// Default primary credential for API requests.
    /// </summary>
    public PrimaryGrantKind Api { get; set; } = PrimaryGrantKind.Stateless;

    internal UAuthPrimaryCredentialPolicy Clone() => new()
    {
        Ui = Ui,
        Api = Api
    };
}
