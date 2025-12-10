namespace CodeBeam.UltimateAuth.Core.Events
{
    public class UAuthEvents
    {
        public Func<IAuthEventContext, Task>? OnAnyEvent { get; set; }

        public Func<SessionCreatedContext<object>, Task>? OnSessionCreated { get; set; }
        public Func<SessionRefreshedContext<object>, Task>? OnSessionRefreshed { get; set; }
        public Func<SessionRevokedContext<object>, Task>? OnSessionRevoked { get; set; }
        public Func<UserLoggedInContext<object>, Task>? OnUserLoggedIn { get; set; }
        public Func<UserLoggedOutContext<object>, Task>? OnUserLoggedOut { get; set; }
    }
}
