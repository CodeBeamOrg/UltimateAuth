using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Errors
{
    public sealed class UAuthSessionNotFoundException
    : UAuthSessionException
    {
        public UAuthSessionNotFoundException(AuthSessionId sessionId)
            : base(sessionId, $"Session '{sessionId}' was not found.")
        {
        }
    }
}
