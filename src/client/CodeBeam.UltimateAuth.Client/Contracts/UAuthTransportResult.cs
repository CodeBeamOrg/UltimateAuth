using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeBeam.UltimateAuth.Client.Contracts;

/// <summary>
/// Represents the transport-level result of an UltimateAuth client request.
/// </summary>
/// <remarks>
/// This type describes the response received by the client transport layer.
/// Application-level authentication results may be represented separately by more specific UltimateAuth result types.
/// </remarks>
public sealed class UAuthTransportResult
{
    /// <summary>
    /// Gets a value indicating whether the transport response represents a successful operation.
    /// </summary>
    [JsonPropertyName("ok")]
    public bool Ok { get; init; }

    /// <summary>
    /// Gets the HTTP status code returned by the request.
    /// </summary>
    [JsonPropertyName("status")]
    public int Status { get; init; }

    /// <summary>
    /// Gets the refresh outcome reported by the transport response, when available.
    /// </summary>
    [JsonPropertyName("refreshOutcome")]
    public string? RefreshOutcome { get; init; }

    /// <summary>
    /// Gets the response body as JSON, when a body is available.
    /// </summary>
    [JsonPropertyName("body")]
    public JsonElement? Body { get; init; }
}
