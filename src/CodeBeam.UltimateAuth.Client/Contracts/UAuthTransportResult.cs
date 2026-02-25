using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeBeam.UltimateAuth.Client.Contracts;

public sealed class UAuthTransportResult
{
    [JsonPropertyName("ok")]
    public bool Ok { get; init; }

    [JsonPropertyName("status")]
    public int Status { get; init; }

    [JsonPropertyName("refreshOutcome")]
    public string? RefreshOutcome { get; init; }

    [JsonPropertyName("body")]
    public JsonElement? Body { get; init; }
}
