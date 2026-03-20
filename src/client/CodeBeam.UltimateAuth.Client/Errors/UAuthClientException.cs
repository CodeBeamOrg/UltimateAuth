namespace CodeBeam.UltimateAuth.Client.Errors;

using CodeBeam.UltimateAuth.Core.Errors;

public abstract class UAuthClientException : UAuthException
{
    protected UAuthClientException(string code, string message) : base(code, message)
    {
    }

    protected UAuthClientException(string code, string message, Exception? inner) : base(code, message, inner)
    {
    }
}
