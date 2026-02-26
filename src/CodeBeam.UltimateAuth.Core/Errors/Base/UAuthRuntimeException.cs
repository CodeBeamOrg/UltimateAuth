namespace CodeBeam.UltimateAuth.Core.Errors;

public abstract class UAuthRuntimeException : UAuthException
{
    public virtual int StatusCode => 400;

    public virtual string Title => "A request error occurred.";

    public virtual string TypePrefix => "https://docs.ultimateauth.com/errors";

    protected UAuthRuntimeException(string code, string message) : base(code, message)
    {
    }
}
