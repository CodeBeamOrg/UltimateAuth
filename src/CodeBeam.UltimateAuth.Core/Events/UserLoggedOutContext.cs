namespace CodeBeam.UltimateAuth.Core.Events
{
    /// <summary>
    /// Represents contextual data emitted when a user logs out of the system.
    /// 
    /// This event is triggered when a logout operation is executed — either by explicit
    /// user action, automatic revocation, administrative force-logout, or tenant-level
    /// security policies.
    /// 
    /// Unlike <see cref="SessionRevokedContext{TUserId}"/>, which targets a specific
    /// session, this event reflects a higher-level “user has logged out” state and may
    /// represent logout from a single session or all sessions depending on the workflow.
    ///
    /// Typical use cases include:
    /// - audit logging of logout activities
    /// - updating user presence or activity services
    /// - triggering notifications (e.g., “You have logged out from device X”)
    /// - integrating with analytics or SIEM systems
    /// </summary>
    public sealed class UserLoggedOutContext<TUserId> : IAuthEventContext
    {
        /// <summary>
        /// Gets the identifier of the user who has logged out.
        /// </summary>
        public TUserId UserId { get; }

        /// <summary>
        /// Gets the timestamp at which the logout occurred.
        /// </summary>
        public DateTimeOffset LoggedOutAt { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="UserLoggedOutContext{TUserId}"/> class.
        /// </summary>
        public UserLoggedOutContext(TUserId userId, DateTimeOffset at)
        {
            UserId = userId;
            LoggedOutAt = at;
        }
    }
}
