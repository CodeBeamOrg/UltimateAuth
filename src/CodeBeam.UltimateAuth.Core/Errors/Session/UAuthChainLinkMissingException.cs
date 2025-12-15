using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Errors
{
    public sealed class UAuthSessionChainLinkMissingException : UAuthSessionException
    {
        public UAuthSessionChainLinkMissingException(AuthSessionId sessionId)
            : base(
                sessionId,
                $"Session '{sessionId}' is not associated with any session chain.")
        {
        }
    }
}
