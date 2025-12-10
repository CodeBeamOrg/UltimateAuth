namespace CodeBeam.UltimateAuth.Core.Errors
{
    public sealed class UAuthInvalidCredentialsException : UAuthDomainException
    {
        public UAuthInvalidCredentialsException() : base("Invalid username or password.")
        {
        }
    }
}
