namespace CodeBeam.UltimateAuth.Core.Errors;

public abstract class UAuthException : Exception
{
    public string Code { get; }

    protected UAuthException(string code, string message) : base(message)
    {
        Code = code;
    }

    protected UAuthException(string code, string message, Exception? inner) : base(message, inner)
    {
        Code = code;
    }
}
