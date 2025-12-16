using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed class SessionValidationResult<TUserId>
    {
        public SessionState State { get; }
        public ISession<TUserId>? Session { get; }
        public ISessionChain<TUserId>? Chain { get; }
        public ISessionRoot<TUserId>? Root { get; }

        private SessionValidationResult(
        SessionState state,
        ISession<TUserId>? session,
        ISessionChain<TUserId>? chain,
        ISessionRoot<TUserId>? root)
        {
            State = state;
            Session = session;
            Chain = chain;
            Root = root;
        }

        public bool IsValid => State == SessionState.Active;

        public static SessionValidationResult<TUserId> Active(
            ISession<TUserId> session,
            ISessionChain<TUserId> chain,
            ISessionRoot<TUserId> root)
            => new(
                SessionState.Active,
                session,
                chain,
                root);

        public static SessionValidationResult<TUserId> Invalid(
            SessionState state)
            => new(
                state,
                session: null,
                chain: null,
                root: null);
    }
}
