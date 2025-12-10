namespace CodeBeam.UltimateAuth.Core.Events
{
    /// <summary>
    /// Represents contextual data emitted when a user successfully completes the login process.
    /// 
    /// This event is triggered after the authentication workflow validates credentials
    /// (or external identity provider assertions) and before or after the session creation step,
    /// depending on pipeline configuration.
    /// 
    /// Typical use cases include:
    /// - auditing successful logins
    /// - triggering login notifications
    /// - updating user activity dashboards
    /// - integrating with SIEM or monitoring systems
    /// 
    /// NOTE:
    /// This event is distinct from <see cref="SessionCreatedContext{TUserId}"/>.
    /// A user may log in without creating a new session (e.g., external SSO),
    /// or multiple sessions may be created after a single login depending on client application flows.
    /// </summary>
    public sealed class UserLoggedInContext<TUserId> : IAuthEventContext
    {
        /// <summary>
        /// Gets the identifier of the user who has logged in.
        /// </summary>
        public TUserId UserId { get; }

        /// <summary>
        /// Gets the timestamp at which the login event occurred.
        /// </summary>
        public DateTime LoggedInAt { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserLoggedInContext{TUserId}"/> class.
        /// </summary>
        public UserLoggedInContext(TUserId userId, DateTime at)
        {
            UserId = userId;
            LoggedInAt = at;
        }
    }
}
