namespace CodeBeam.UltimateAuth.Core.Errors
{
    public abstract class UAuthDomainException : UAuthException
    {
        protected UAuthDomainException(string message) : base(message) { }
    }
}
