using System.Text.Json.Serialization;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed class PkceCompleteRequest
{
    [JsonPropertyName("authorization_code")]
    public string AuthorizationCode { get; init; } = default!;

    [JsonPropertyName("code_verifier")]
    public string CodeVerifier { get; init; } = default!;


    public string Identifier { get; init; } = default!;
    public string Secret { get; init; } = default!;

    [JsonPropertyName("return_url")]
    public string ReturnUrl { get; init; } = default!;

    [JsonPropertyName("hub_session_id")]
    public string HubSessionId { get; init; } = default!;
}
