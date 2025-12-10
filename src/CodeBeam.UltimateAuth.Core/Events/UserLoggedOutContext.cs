namespace CodeBeam.UltimateAuth.Core.Events
{
    public sealed class UserLoggedOutContext<TUserId> : IAuthEventContext
    {
        public TUserId UserId { get; }
        public DateTime LoggedOutAt { get; }

        public UserLoggedOutContext(TUserId userId, DateTime at)
        {
            UserId = userId;
            LoggedOutAt = at;
        }

    }
}
