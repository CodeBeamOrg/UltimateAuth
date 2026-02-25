namespace CodeBeam.UltimateAuth.Core.Errors;

public abstract class UAuthRuntimeException : UAuthException
{
    protected UAuthRuntimeException(string code, string message) : base(code, message)
    {
    }
}
