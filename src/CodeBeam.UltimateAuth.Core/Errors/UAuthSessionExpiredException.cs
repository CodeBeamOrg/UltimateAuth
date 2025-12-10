using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Errors
{
    public sealed class UAuthSessionExpiredException : UAuthSessionException
    {
        public UAuthSessionExpiredException(AuthSessionId sessionId) : base(sessionId, $"Session '{sessionId}' has expired.")
        {
        }
    }
}
