using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Models
{
    public sealed class SessionValidationResult<TUserId>
    {
        public ISession<TUserId>? Session { get; init; }
        public ISessionChain<TUserId>? Chain { get; init; }
        public ISessionRoot<TUserId>? Root { get; init; }

        public SessionState State { get; init; }
    }

}
