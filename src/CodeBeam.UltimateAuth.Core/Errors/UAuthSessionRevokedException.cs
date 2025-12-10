using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Errors
{
    public sealed class UAuthSessionRevokedException : UAuthSessionException
    {
        public UAuthSessionRevokedException(AuthSessionId sessionId) : base(sessionId, $"Session '{sessionId}' has been revoked.")
        {
        }
    }
}
