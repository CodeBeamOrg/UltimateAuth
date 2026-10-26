using System.Text.Json.Serialization;

namespace CodeBeam.UltimateAuth.Client.Contracts;

/// <summary>
/// Represents an HTTP response from an application resource.
/// </summary>
public sealed class UAuthResourceTransportResult
{
    [JsonPropertyName("ok")]
    public bool Ok { get; init; }

    [JsonPropertyName("status")]
    public int Status { get; init; }

    [JsonPropertyName("body")]
    public string? Body { get; init; }

    [JsonPropertyName("contentType")]
    public string? ContentType { get; init; }
}
