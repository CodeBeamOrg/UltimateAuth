namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public interface IPkceAuthorizationValidator
{
    PkceValidationResult Validate(PkceAuthorizationArtifact artifact, string codeVerifier, PkceContextSnapshot completionContext, DateTimeOffset now);
}
