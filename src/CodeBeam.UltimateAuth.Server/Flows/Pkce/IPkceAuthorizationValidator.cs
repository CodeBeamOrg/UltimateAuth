namespace CodeBeam.UltimateAuth.Server.Flows;

public interface IPkceAuthorizationValidator
{
    PkceValidationResult Validate(PkceAuthorizationArtifact artifact, string codeVerifier, PkceContextSnapshot completionContext, DateTimeOffset now);
}
