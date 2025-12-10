namespace CodeBeam.UltimateAuth.Core.Errors
{
    public sealed class UAuthSecurityVersionMismatchException : UAuthDomainException
    {
        public long SessionVersion { get; }
        public long UserVersion { get; }

        public UAuthSecurityVersionMismatchException(long sessionVersion, long userVersion) : base($"Security version mismatch. Session={sessionVersion}, User={userVersion}")
        {
            SessionVersion = sessionVersion;
            UserVersion = userVersion;
        }

    }
}
