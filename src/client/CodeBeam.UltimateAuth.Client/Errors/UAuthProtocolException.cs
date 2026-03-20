namespace CodeBeam.UltimateAuth.Client.Errors;

public sealed class UAuthProtocolException : UAuthClientException
{
    public UAuthProtocolException(string message) : base("protocol_error", message)
    {
    }

    public UAuthProtocolException(string message, Exception? inner) : base("protocol_error", message, inner)
    {
    }
}
