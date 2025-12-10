namespace CodeBeam.UltimateAuth.Core.Events
{
    internal sealed class UAuthEventDispatcher
    {
        private readonly UAuthEvents _events;

        public UAuthEventDispatcher(UAuthEvents events)
        {
            _events = events;
        }

        public async Task DispatchAsync(IAuthEventContext context)
        {
            if (_events.OnAnyEvent is not null)
                await SafeInvoke(() => _events.OnAnyEvent(context));

            switch (context)
            {
                case SessionCreatedContext<object> c:
                    if (_events.OnSessionCreated != null)
                        await SafeInvoke(() => _events.OnSessionCreated(c));
                    break;

                case SessionRefreshedContext<object> c:
                    if (_events.OnSessionRefreshed != null)
                        await SafeInvoke(() => _events.OnSessionRefreshed(c));
                    break;

                case SessionRevokedContext<object> c:
                    if (_events.OnSessionRevoked != null)
                        await SafeInvoke(() => _events.OnSessionRevoked(c));
                    break;

                case UserLoggedInContext<object> c:
                    if (_events.OnUserLoggedIn != null)
                        await SafeInvoke(() => _events.OnUserLoggedIn(c));
                    break;

                case UserLoggedOutContext<object> c:
                    if (_events.OnUserLoggedOut != null)
                        await SafeInvoke(() => _events.OnUserLoggedOut(c));
                    break;
            }
        }

        private static async Task SafeInvoke(Func<Task> func)
        {
            try { await func(); }
            catch { /* swallow → event hook must not break auth flow */ }
        }

    }
}
