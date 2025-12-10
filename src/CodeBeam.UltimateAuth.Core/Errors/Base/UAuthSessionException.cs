using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Errors
{
    public abstract class UAuthSessionException : UAuthDomainException
    {
        public AuthSessionId SessionId { get; }

        protected UAuthSessionException(AuthSessionId sessionId, string message) : base(message)
        {
            SessionId = sessionId;
        }
    }
}
