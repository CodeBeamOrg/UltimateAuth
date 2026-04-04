using CodeBeam.UltimateAuth.Core.Infrastructure;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Flows;
using CodeBeam.UltimateAuth.Server.Stores;
using System.Security.Cryptography;
using System.Text;

namespace CodeBeam.UltimateAuth.Tests.Unit.Helpers;

internal static class TestPkceFactory
{
    public static (PkceAuthorizationArtifact Artifact, string Verifier) Create(
        UAuthClientProfile profile = UAuthClientProfile.BlazorWasm,
        TenantKey? tenant = null)
    {
        tenant ??= TenantKeys.Single;

        var verifier = "test_verifier_123";

        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.ASCII.GetBytes(verifier));
        var challenge = Base64Url.Encode(hash);

        var key = AuthArtifactKey.New();

        var snapshot = new PkceContextSnapshot(
            clientProfile: profile,
            tenant: (TenantKey)tenant,
            redirectUri: "/",
            device: TestDevice.Default()
        );

        var artifact = new PkceAuthorizationArtifact(
            authorizationCode: key,
            codeChallenge: challenge,
            challengeMethod: PkceChallengeMethod.S256,
            expiresAt: DateTimeOffset.UtcNow.AddMinutes(5),
            context: snapshot
        );

        return (artifact, verifier);
    }
}
