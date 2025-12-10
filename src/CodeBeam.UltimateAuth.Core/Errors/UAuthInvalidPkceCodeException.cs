namespace CodeBeam.UltimateAuth.Core.Errors
{
    public sealed class UAuthInvalidPkceCodeException : UAuthDomainException
    {
        public UAuthInvalidPkceCodeException() : base("Invalid PKCE authorization code.")
        {
        }
    }
}
