using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Client.Infrastructure;

/// <summary>
/// Defines a client for sending requests to the UltimateAuth server, handling navigation, form submissions, JSON payloads, and transactional operations.
/// </summary>
public interface IUAuthRequestClient
{
    /// <summary>
    /// Navigates to the specified endpoint, optionally submitting form data, and handles the response.
    /// </summary>
    /// <param name="endpoint"></param>
    /// <param name="form"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task NavigateAsync(string endpoint, IDictionary<string, string>? form = null, CancellationToken ct = default);


    /// <summary>
    /// Sends a form submission to the specified endpoint and returns the result of the operation.
    /// </summary>
    /// <param name="endpoint"></param>
    /// <param name="form"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<UAuthTransportResult> SendFormAsync(string endpoint, IDictionary<string, string>? form = null, CancellationToken ct = default);


    /// <summary>
    /// Sends a JSON payload to the specified endpoint and returns the result of the operation.
    /// </summary>
    /// <param name="endpoint"></param>
    /// <param name="payload"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<UAuthTransportResult> SendJsonAsync(string endpoint, object? payload = null, CancellationToken ct = default);


    /// <summary>
    /// Attempts to perform a transactional operation by first trying the specified endpoint and, if successful, committing the operation to another endpoint. Returns the result of the try operation.
    /// </summary>
    /// <typeparam name="TTryResult"></typeparam>
    /// <param name="tryEndpoint"></param>
    /// <param name="commitEndpoint"></param>
    /// <param name="request"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    Task<TTryResult> TryAndCommitAsync<TTryResult>(string tryEndpoint, string commitEndpoint, object request, CancellationToken ct = default);
}
