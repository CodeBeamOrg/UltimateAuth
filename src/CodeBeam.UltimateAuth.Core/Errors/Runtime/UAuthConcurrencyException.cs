namespace CodeBeam.UltimateAuth.Core.Errors;

public sealed class UAuthConcurrencyException : UAuthRuntimeException
{
    public override int StatusCode => 409;

    public override string Title => "The resource was modified by another process.";

    public override string TypePrefix => "https://docs.ultimateauth.com/errors/concurrency";

    public UAuthConcurrencyException(string code = "concurrency_conflict") : base(code, code)
    {
    }
}
