using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Errors
{
    public sealed class UAuthSessionInvalidStateException : UAuthSessionException
    {
        public SessionState State { get; }

        public UAuthSessionInvalidStateException(
            AuthSessionId sessionId,
            SessionState state)
            : base(
                sessionId,
                $"Session '{sessionId}' is in invalid state '{state}'.")
        {
            State = state;
        }
    }
}
