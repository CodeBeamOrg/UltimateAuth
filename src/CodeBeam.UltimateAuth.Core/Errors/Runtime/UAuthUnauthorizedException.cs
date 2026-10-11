namespace CodeBeam.UltimateAuth.Core.Errors;

public sealed class UAuthUnauthorizedException : UAuthRuntimeException
{
    public override int StatusCode => 401;

    public UAuthUnauthorizedException(string code = "unauthorized") : base(code, "Unauthorized.")
    {
    }
}
