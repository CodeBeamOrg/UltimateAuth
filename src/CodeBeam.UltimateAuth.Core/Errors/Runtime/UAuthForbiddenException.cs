namespace CodeBeam.UltimateAuth.Core.Errors;

public sealed class UAuthForbiddenException : UAuthRuntimeException
{
    public UAuthForbiddenException(string code = "forbidden") : base(code, "Forbidden.")
    {
    }
}
