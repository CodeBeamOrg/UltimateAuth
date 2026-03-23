using CodeBeam.UltimateAuth.Core.Infrastructure;
using System.Security.Cryptography;
using System.Text;

namespace CodeBeam.UltimateAuth.Server.Flows;

internal sealed class PkceAuthorizationValidator : IPkceAuthorizationValidator
{
    public PkceValidationResult Validate(PkceAuthorizationArtifact artifact, string codeVerifier, PkceContextSnapshot completionContext, DateTimeOffset now)
    {
        if (artifact.IsExpired(now))
            return PkceValidationResult.Fail(PkceValidationFailureReason.ArtifactExpired);

        if (!IsContextValid(artifact.Context, completionContext))
            return PkceValidationResult.Fail(PkceValidationFailureReason.ContextMismatch);

        if (artifact.ChallengeMethod != PkceChallengeMethod.S256)
            return PkceValidationResult.Fail(PkceValidationFailureReason.UnsupportedChallengeMethod);

        if (!IsVerifierValid(codeVerifier, artifact.CodeChallenge))
            return PkceValidationResult.Fail(PkceValidationFailureReason.InvalidVerifier);

        return PkceValidationResult.Ok();
    }

    private static bool IsContextValid(PkceContextSnapshot original, PkceContextSnapshot completion)
    {
        // TODO: Fix this
        //if (!original.ClientProfile.Equals(completion.ClientProfile))
        //    return false;

        if (!string.Equals(original.Tenant, completion.Tenant, StringComparison.Ordinal))
            return false;

        if (!string.Equals(original.RedirectUri, completion.RedirectUri, StringComparison.Ordinal))
            return false;

        if (!Equals(original.Device, completion.Device))
            return false;

        return true;
    }

    private static bool IsVerifierValid(string verifier, string expectedChallenge)
    {
        if (string.IsNullOrWhiteSpace(verifier))
            return false;

        using var sha256 = SHA256.Create();
        byte[] hash = sha256.ComputeHash(Encoding.ASCII.GetBytes(verifier));

        string computedChallenge = Base64Url.Encode(hash);

        return CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(computedChallenge), Encoding.ASCII.GetBytes(expectedChallenge));
    }
}
