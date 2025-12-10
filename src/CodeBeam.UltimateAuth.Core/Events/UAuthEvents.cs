namespace CodeBeam.UltimateAuth.Core.Events
{
    /// <summary>
    /// Provides an optional, application-wide event hook system for UltimateAuth.
    /// 
    /// This class allows consumers to attach callbacks to authentication-related events
    /// without implementing a full event bus or subscribing via DI. All handlers here are
    /// optional and are executed after the corresponding operation completes successfully.
    /// 
    /// IMPORTANT:
    /// These delegates are designed for lightweight reactions such as:
    /// - logging
    /// - metrics
    /// - notifications
    /// - auditing
    /// Custom business workflows **should not** be implemented here; instead, use dedicated
    /// application services or a domain event bus for complex logic.
    ///
    /// All handlers receive an <see cref="IAuthEventContext"/> instance. The generic
    /// type parameter is normalized as <c>object</c> to allow uniform handling regardless
    /// of the actual TUserId type used by the application.
    /// </summary>
    public class UAuthEvents
    {
        /// <summary>
        /// Fired on every auth-related event.
        /// This global hook allows logging, tracing or metrics pipelines to observe all events.
        /// </summary>
        public Func<IAuthEventContext, Task>? OnAnyEvent { get; set; }

        /// <summary>
        /// Fired when a new session is created (login or device bootstrap).
        /// </summary>
        public Func<SessionCreatedContext<object>, Task>? OnSessionCreated { get; set; }

        /// <summary>
        /// Fired when an existing session is refreshed and rotated.
        /// </summary>
        public Func<SessionRefreshedContext<object>, Task>? OnSessionRefreshed { get; set; }

        /// <summary>
        /// Fired when a specific session is revoked.
        /// </summary>
        public Func<SessionRevokedContext<object>, Task>? OnSessionRevoked { get; set; }

        /// <summary>
        /// Fired when a user successfully completes the login process.
        /// Note: separate from SessionCreated; this is a higher-level event.
        /// </summary>
        public Func<UserLoggedInContext<object>, Task>? OnUserLoggedIn { get; set; }

        /// <summary>
        /// Fired when a user logs out or all sessions for the user are revoked.
        /// </summary>
        public Func<UserLoggedOutContext<object>, Task>? OnUserLoggedOut { get; set; }
    }
}
