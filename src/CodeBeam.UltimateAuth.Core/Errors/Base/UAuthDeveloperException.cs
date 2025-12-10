namespace CodeBeam.UltimateAuth.Core.Errors
{
    public abstract class UAuthDeveloperException : UAuthException
    {
        protected UAuthDeveloperException(string message) : base(message) { }
    }
}
