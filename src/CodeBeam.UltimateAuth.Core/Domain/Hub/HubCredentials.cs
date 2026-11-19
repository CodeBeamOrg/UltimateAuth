using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Core.Domain;

public sealed class HubCredentials
{
    public string AuthorizationCode { get; init; } = default!;
    public string CodeVerifier { get; init; } = default!;
    public UAuthApplicationProfile ClientProfile { get; init; }
}
