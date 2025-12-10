namespace CodeBeam.UltimateAuth.Core.Events
{
    public sealed class UserLoggedInContext<TUserId> : IAuthEventContext
    {
        public TUserId UserId { get; }
        public DateTime LoggedInAt { get; }

        public UserLoggedInContext(TUserId userId, DateTime at)
        {
            UserId = userId;
            LoggedInAt = at;
        }

    }
}
