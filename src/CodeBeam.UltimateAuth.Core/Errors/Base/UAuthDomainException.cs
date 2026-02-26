namespace CodeBeam.UltimateAuth.Core.Errors;

public abstract class UAuthDomainException : UAuthException
{
    protected UAuthDomainException(string code, string message) : base(code, message)
    {
    }
}