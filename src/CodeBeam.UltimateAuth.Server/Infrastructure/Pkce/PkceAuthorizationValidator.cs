using CodeBeam.UltimateAuth.Core.Infrastructure;
using System.Security.Cryptography;
using System.Text;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

internal sealed class PkceAuthorizationValidator : IPkceAuthorizationValidator
{
    public PkceValidationResult Validate(PkceAuthorizationArtifact artifact, string codeVerifier, PkceContextSnapshot completionContext, DateTimeOffset now)
    {
        // 1️⃣ Expiration
        if (artifact.IsExpired(now))
            return PkceValidationResult.Fail(PkceValidationFailureReason.ArtifactExpired);

        // 2️⃣ Attempt limit
        if (!artifact.CanAttempt())
            return PkceValidationResult.Fail(PkceValidationFailureReason.MaxAttemptsExceeded);

        // 3️⃣ Context consistency
        //if (!IsContextValid(artifact.Context, completionContext))
            //return PkceValidationResult.Fail(PkceValidationFailureReason.ContextMismatch);

        // 4️⃣ Challenge method
        if (artifact.ChallengeMethod != PkceChallengeMethod.S256)
            return PkceValidationResult.Fail(PkceValidationFailureReason.UnsupportedChallengeMethod);

        // 5️⃣ Verifier check
        if (!IsVerifierValid(codeVerifier, artifact.CodeChallenge))
            return PkceValidationResult.Fail(PkceValidationFailureReason.InvalidVerifier);

        return PkceValidationResult.Ok();
    }

    private static bool IsContextValid(PkceContextSnapshot original, PkceContextSnapshot completion)
    {
        if (!original.ClientProfile.Equals(completion.ClientProfile))
            return false;

        if (!string.Equals(original.TenantId, completion.TenantId, StringComparison.Ordinal))
            return false;

        if (!string.Equals(original.RedirectUri, completion.RedirectUri, StringComparison.Ordinal))
            return false;

        if (!string.Equals(original.DeviceId, completion.DeviceId, StringComparison.Ordinal))
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
