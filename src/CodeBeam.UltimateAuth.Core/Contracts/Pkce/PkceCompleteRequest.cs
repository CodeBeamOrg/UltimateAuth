using CodeBeam.UltimateAuth.Core.Defaults;
using System.Text.Json.Serialization;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed record PkceCompleteRequest
{
    [JsonPropertyName("authorization_code")]
    public required string AuthorizationCode { get; init; }

    [JsonPropertyName("code_verifier")]
    public required string CodeVerifier { get; init; }

    public required string Identifier { get; init; }

    public required string Secret { get; init; }

    [JsonPropertyName(UAuthConstants.Form.ReturnUrl)]
    public string? ReturnUrl { get; init; }

    [JsonPropertyName("hub_session_id")]
    public string? HubSessionId { get; init; }
}
