namespace CodeBeam.UltimateAuth.Core.Errors
{
    public sealed class UAuthRootRevokedException : UAuthDomainException
    {
        public UAuthRootRevokedException() : base("User root has been revoked. All sessions are invalid.")
        {
        }
    }
}
