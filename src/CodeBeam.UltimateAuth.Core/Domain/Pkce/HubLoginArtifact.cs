namespace CodeBeam.UltimateAuth.Core.Domain;

public sealed class HubLoginArtifact : AuthArtifact
{
    public string AuthorizationCode { get; }
    public string CodeVerifier { get; }

    public HubLoginArtifact(
        string authorizationCode,
        string codeVerifier,
        DateTimeOffset expiresAt,
        int maxAttempts = 3)
        : base(AuthArtifactType.HubLogin, expiresAt, maxAttempts)
    {
        AuthorizationCode = authorizationCode;
        CodeVerifier = codeVerifier;
    }
}
