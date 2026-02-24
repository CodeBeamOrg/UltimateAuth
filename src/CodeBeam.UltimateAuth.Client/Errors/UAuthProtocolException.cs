using CodeBeam.UltimateAuth.Core.Errors;

namespace CodeBeam.UltimateAuth.Client.Errors;

/// <summary>
/// Represents a protocol-level failure in the UltimateAuth client.
/// Thrown when the server response does not conform to the expected contract,
/// such as invalid JSON, missing required fields, or unexpected response structure.
/// </summary>
public sealed class UAuthProtocolException : UAuthException
{
    public UAuthProtocolException(string message) : base(message)
    {
    }

    public UAuthProtocolException(string message, Exception? inner) : base(message, inner)
    {
    }
}