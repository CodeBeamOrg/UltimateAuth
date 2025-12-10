namespace CodeBeam.UltimateAuth.Core.Errors
{
    public abstract class UAuthException : Exception
    {
        protected UAuthException(string message) : base(message) { }

        protected UAuthException(string message, Exception? inner) : base(message, inner) { }
    }
}
