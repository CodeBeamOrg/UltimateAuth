namespace CodeBeam.UltimateAuth.Core.Errors;

public sealed class UAuthValidationException : UAuthRuntimeException
{
    public UAuthValidationException(string code) : base(code, "Validation failed.")
    {
    }
}
