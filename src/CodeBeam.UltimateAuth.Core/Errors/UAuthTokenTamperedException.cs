namespace CodeBeam.UltimateAuth.Core.Errors
{
    public sealed class UAuthTokenTamperedException : UAuthDomainException
    {
        public UAuthTokenTamperedException() : base("Token integrity check failed (possible tampering).")
        {
        }
    }
}
