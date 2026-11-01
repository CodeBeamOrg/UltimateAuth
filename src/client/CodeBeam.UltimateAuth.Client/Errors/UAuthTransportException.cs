using System.Net;

namespace CodeBeam.UltimateAuth.Client.Errors;

public sealed class UAuthTransportException : UAuthClientException
{
    public HttpStatusCode? StatusCode { get; }
    public TimeSpan? RetryAfter { get; init; }

    public UAuthTransportException(string message) : base("transport_error", message)
    {
    }

    public UAuthTransportException(string message, Exception? inner) : base("transport_error", message, inner)
    {
    }

    public UAuthTransportException(string message, HttpStatusCode statusCode) : base(statusCode == HttpStatusCode.TooManyRequests ? "rate_limited" : "transport_error", message)
    {
        StatusCode = statusCode;
    }

    public UAuthTransportException(string message, HttpStatusCode statusCode, Exception? inner) : base(statusCode == HttpStatusCode.TooManyRequests ? "rate_limited" : "transport_error", message, inner)
    {
        StatusCode = statusCode;
    }
}
