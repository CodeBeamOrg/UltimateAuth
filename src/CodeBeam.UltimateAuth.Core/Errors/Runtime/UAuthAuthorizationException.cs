namespace CodeBeam.UltimateAuth.Core.Errors;

public sealed class UAuthAuthorizationException : UAuthRuntimeException
{
    public override int StatusCode => 403;

    public override string Title => "Forbidden";

    public override string TypePrefix => "https://docs.ultimateauth.com/errors/authorization";

    public UAuthAuthorizationException(string code = "You do not have permission to perform this action.") : base(code, code)
    {
    }
}
