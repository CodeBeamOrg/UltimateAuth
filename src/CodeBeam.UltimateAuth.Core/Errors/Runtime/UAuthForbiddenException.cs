namespace CodeBeam.UltimateAuth.Core.Errors;

public sealed class UAuthForbiddenException : UAuthRuntimeException
{
    public override int StatusCode => 403;

    public UAuthForbiddenException(string code = "forbidden") : base(code, "Forbidden.")
    {
    }
}
