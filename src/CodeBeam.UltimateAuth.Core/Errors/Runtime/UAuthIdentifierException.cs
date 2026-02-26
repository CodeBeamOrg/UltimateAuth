namespace CodeBeam.UltimateAuth.Core.Errors;

public abstract class UAuthIdentifierException : UAuthRuntimeException
{
    public override string Title => "User identifier operation failed.";
    public override string TypePrefix => "https://docs.ultimateauth.com/errors/identifiers";

    protected UAuthIdentifierException(string code, string message) : base(code, message)
    {
    }
}

public sealed class UAuthIdentifierConflictException : UAuthIdentifierException
{
    public override int StatusCode => 409;
    public UAuthIdentifierConflictException(string code, string? message = null)
        : base(code, message ?? code) { }
}

public sealed class UAuthIdentifierValidationException : UAuthIdentifierException
{
    public override int StatusCode => 400;
    public UAuthIdentifierValidationException(string code, string? message = null)
        : base(code, message ?? code) { }
}

public sealed class UAuthIdentifierNotFoundException : UAuthIdentifierException
{
    public override int StatusCode => 404;
    public UAuthIdentifierNotFoundException(string code = "identifier_not_found", string? message = null)
        : base(code, message ?? code) { }
}
