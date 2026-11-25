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

    /// <summary>Raw Retry-After header, when exposed by the server/CORS policy.</summary>
    [JsonPropertyName("retryAfter")]
    public string? RetryAfter { get; init; }

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
    /// Raw access credential returned by the transport, when the configured delivery mechanism exposes it.
    /// </summary>
    [JsonPropertyName("accessToken")]
    public string? AccessToken { get; init; }

    /// <summary>
    /// Access token expiration metadata supplied by the issuer.
    /// </summary>
    [JsonPropertyName("accessTokenExpiresAt")]
    public string? AccessTokenExpiresAt { get; init; }

    [JsonPropertyName("accessTokenFormat")]
    public string? AccessTokenFormat { get; init; }

    [JsonPropertyName("accessTokenHeaderFormat")]
    public string? AccessTokenHeaderFormat { get; init; }

    /// <summary>
    /// Gets the response body as JSON, when a body is available.
    /// </summary>
    [JsonPropertyName("body")]
    public JsonElement? Body { get; init; }
}
