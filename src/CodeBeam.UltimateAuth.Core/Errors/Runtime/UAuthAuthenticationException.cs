namespace CodeBeam.UltimateAuth.Core.Errors;

public sealed class UAuthAuthenticationException : UAuthRuntimeException
{
    public override int StatusCode => 401;
    public override string Title => "Unauthorized";

    public UAuthAuthenticationException(string code = "authentication_required")
        : base(code, code)
    {
    }
}
