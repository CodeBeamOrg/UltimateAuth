namespace CodeBeam.UltimateAuth.Core.Errors;

public abstract class UAuthDeveloperException : UAuthException
{
    protected UAuthDeveloperException(string code, string message) : base(code, message)
    {
    }
}
