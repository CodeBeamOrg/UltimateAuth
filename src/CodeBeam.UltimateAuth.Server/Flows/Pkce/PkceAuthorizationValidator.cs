using CodeBeam.UltimateAuth.Core.Domain;
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
        // Tenant is part of the server-side security boundary and must
        // remain stable throughout the PKCE transaction.
        if (!string.Equals(original.Tenant, completion.Tenant, StringComparison.Ordinal))
            return false;

        // TODO: Bind the effective client profile rather than the physical
        // completion request profile. In Hub flows the artifact may represent
        // BlazorWasm while the completion request is executed by UAuthHub.
        //if (!original.ClientProfile.Equals(completion.ClientProfile))
        //    return false;

        // TODO: Add protocol-level redirect/return-url binding once the
        // relationship between authorization RedirectUri and Hub ReturnUrl
        // is explicitly defined. They are not currently equivalent concepts.
        //if (!string.Equals(original.RedirectUri, completion.RedirectUri, StringComparison.Ordinal))
        //    return false;

        // TODO: Add logical client-device binding. The physical device context
        // of the Hub completion request is not necessarily the device context
        // captured from the originating client.
        //if (!IsDeviceValid(original.Device, completion.Device))
        //    return false;

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
