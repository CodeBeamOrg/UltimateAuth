namespace CodeBeam.UltimateAuth.Core.Errors
{
    public sealed class UAuthSessionRootRevokedException : Exception
    {
        public object UserId { get; }

        public UAuthSessionRootRevokedException(object userId)
            : base("All sessions for the user have been revoked.")
        {
            UserId = userId;
        }
    }
}
