namespace CodeBeam.UltimateAuth.Core.Errors;

public sealed class UAuthUnauthorizedException : UAuthRuntimeException
{
    public UAuthUnauthorizedException(string code = "unauthorized") : base(code, "Unauthorized.")
    {
    }
}
