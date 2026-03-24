using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Flows;

namespace CodeBeam.UltimateAuth.Tests.Unit.Helpers;

internal static class TestHubFactory
{
    public static HubFlowArtifact Create(PkceAuthorizationArtifact pkce)
    {
        var payload = new HubFlowPayload();
        payload.Set("authorization_code", pkce.AuthorizationCode.Value);
        payload.Set("code_verifier", "test");

        return new HubFlowArtifact(
            HubSessionId.New(),
            HubFlowType.Login,
            pkce.Context.ClientProfile,
            pkce.Context.Tenant,
            TestDevice.Default(),
            "/",
            payload,
            DateTimeOffset.UtcNow.AddMinutes(5)
        );
    }
}
