namespace CodeBeam.UltimateAuth.Core.Errors;

public sealed class UAuthConflictException : UAuthRuntimeException
{
    public UAuthConflictException(string code) : base(code, "A resource conflict occurred.")
    {
    }
}
