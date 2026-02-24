using CodeBeam.UltimateAuth.Core.Errors;
using System.Net;

namespace CodeBeam.UltimateAuth.Client.Errors;

/// <summary>
/// Represents a transport-level failure while communicating with the UltimateAuth server.
/// This includes network errors, HTTP status failures, and connectivity issues.
/// </summary>
public sealed class UAuthTransportException : UAuthException
{
    /// <summary>
    /// Gets the HTTP status code associated with the failure, if available.
    /// </summary>
    public HttpStatusCode? StatusCode { get; }

    public UAuthTransportException(string message) : base(message)
    {
    }

    public UAuthTransportException(string message, Exception? inner) : base(message, inner)
    {
    }

    public UAuthTransportException(string message, HttpStatusCode statusCode) : base(message)
    {
        StatusCode = statusCode;
    }

    public UAuthTransportException(string message, HttpStatusCode statusCode, Exception? inner) : base(message, inner)
    {
        StatusCode = statusCode;
    }
}