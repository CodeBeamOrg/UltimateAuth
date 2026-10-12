namespace CodeBeam.UltimateAuth.Core.Errors;

public sealed class UAuthConflictException : UAuthRuntimeException
{
    public override int StatusCode => 409;

    public UAuthConflictException(string code) : base(code, "A resource conflict occurred.")
    {
    }
}
