namespace CodeBeam.UltimateAuth.Core.Errors;

public class UAuthNotFoundException : UAuthRuntimeException
{
    public override int StatusCode => 400;

    public override string Title => "The resource is not found.";

    public override string TypePrefix => "https://docs.ultimateauth.com/errors/notfound";

    public UAuthNotFoundException(string code = "resource_not_found") : base(code, code)
    {
    }
}
