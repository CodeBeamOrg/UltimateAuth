namespace CodeBeam.UltimateAuth.Core.Errors;

public sealed class UAuthNotFoundException : UAuthRuntimeException
{
    public UAuthNotFoundException(string code) : base(code, "Resource not found.")
    {
    }
}
