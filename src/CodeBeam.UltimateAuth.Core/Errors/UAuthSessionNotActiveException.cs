using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Errors
{
    public sealed class UAuthSessionNotActiveException : UAuthSessionException
    {
        public UAuthSessionNotActiveException(AuthSessionId sessionId) : base(sessionId, $"Session '{sessionId}' is not active.")
        {
        }
    }
}
