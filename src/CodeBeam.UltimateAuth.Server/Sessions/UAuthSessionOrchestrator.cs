using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contexts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Issuers;
using CodeBeam.UltimateAuth.Server.Options;

namespace CodeBeam.UltimateAuth.Server.Sessions
{
    /// <summary>
    /// Default UltimateAuth session store implementation.
    /// Handles session, chain, and root orchestration on top of a kernel store.
    /// </summary>
    public sealed class UAuthSessionOrchestrator<TUserId> : ISessionOrchestrator<TUserId>
    {
        private readonly ISessionStoreFactory _factory;
        private readonly UAuthSessionIssuer<TUserId> _sessionIssuer;
        private readonly UAuthServerOptions _serverOptions;

        public UAuthSessionOrchestrator(ISessionStoreFactory factory, UAuthSessionIssuer<TUserId> sessionIssuer, UAuthServerOptions serverOptions)
        {
            _factory = factory;
            _sessionIssuer = sessionIssuer;
            _serverOptions = serverOptions;
        }

        public async Task<IssuedSession<TUserId>> CreateLoginSessionAsync(SessionIssueContext<TUserId> context)
        {
            var kernel = _factory.Create<TUserId>(context.TenantId);

            // 1️⃣ Load or create root
            var root = await kernel.GetSessionRootAsync(
                context.TenantId,
                context.UserId);

            if (root is null)
            {
                root = UAuthSessionRoot<TUserId>.Create(
                    context.TenantId,
                    context.UserId,
                    context.Now);
            }
            else if (root.IsRevoked)
            {
                throw new InvalidOperationException(
                    "User session root is revoked.");
            }

            // 2️⃣ Load or create chain (interface → concrete)
            ISessionChain<TUserId>? loadedChain = null;

            if (context.ChainId is not null)
            {
                loadedChain = await kernel.GetChainAsync(
                    context.TenantId,
                    context.ChainId.Value);
            }

            if (loadedChain is not null && loadedChain.IsRevoked)
            {
                throw new InvalidOperationException(
                    "Session chain is revoked.");
            }

            UAuthSessionChain<TUserId> chain;

            if (loadedChain is null)
            {
                chain = UAuthSessionChain<TUserId>.Create(
                    ChainId.New(),
                    context.TenantId,
                    context.UserId,
                    root.SecurityVersion,
                    context.ClaimsSnapshot);
            }
            else if (loadedChain is UAuthSessionChain<TUserId> concreteChain)
            {
                chain = concreteChain;
            }
            else
            {
                throw new InvalidOperationException(
                    "Unsupported ISessionChain implementation. " +
                    "UltimateAuth requires SessionChain<TUserId>.");
            }

            // TODO: Add cancellation token support
            var issuedSession = await _sessionIssuer.IssueAsync(
                context,
                chain);

            // 4️⃣ Persist session
            await kernel.SaveSessionAsync(
                context.TenantId,
                issuedSession.Session);

            // 5️⃣ Update & persist chain
            var updatedChain = chain.ActivateSession(
                issuedSession.Session.SessionId);

            await kernel.SaveChainAsync(
                context.TenantId,
                updatedChain);

            // 6️⃣ Persist root (idempotent)
            await kernel.SaveSessionRootAsync(
                context.TenantId,
                root);

            return issuedSession;
        }

        public async Task<IssuedSession<TUserId>> RotateSessionAsync(string? tenantId, AuthSessionId currentSessionId, SessionIssueContext<TUserId> context)
        {
            if (_serverOptions.Mode == UAuthMode.PureJwt)
                throw new InvalidOperationException(
                    "Session rotation is not available in PureJwt mode.");

            var kernel = _factory.Create<TUserId>(tenantId);

            // 1️⃣ Load current session
            var currentSession = await kernel.GetSessionAsync(
                tenantId,
                currentSessionId);

            if (currentSession is null)
                throw new InvalidOperationException("Session not found.");

            if (currentSession.IsRevoked)
                throw new InvalidOperationException("Session is revoked.");

            if (currentSession.GetState(context.Now) != SessionState.Active)
                throw new InvalidOperationException("Session is not active.");

            // 2️⃣ Load chain id
            var chainId = await kernel.GetChainIdBySessionAsync(
                tenantId,
                currentSessionId);

            if (chainId is null)
                throw new InvalidOperationException("Session chain not found.");

            // 3️⃣ Load chain
            var loadedChain = await kernel.GetChainAsync(
                tenantId,
                chainId.Value);

            if (loadedChain is null || loadedChain.IsRevoked)
                throw new InvalidOperationException("Session chain is revoked.");

            if (loadedChain is not UAuthSessionChain<TUserId> chain)
                throw new InvalidOperationException(
                    "Unsupported ISessionChain implementation.");

            // 4️⃣ Load root
            var root = await kernel.GetSessionRootAsync(
                tenantId,
                context.UserId);

            if (root is null || root.IsRevoked)
                throw new InvalidOperationException("Session root is revoked.");

            // 5️⃣ Security version check
            if (currentSession.SecurityVersionAtCreation != root.SecurityVersion)
                throw new InvalidOperationException(
                    "Session security version mismatch.");

            // TODO: Add cancellation token support
            var issuedSession = await _sessionIssuer.IssueAsync(
                context,
                chain);

            // 7️⃣ Persist new session
            await kernel.SaveSessionAsync(
                tenantId,
                issuedSession.Session);

            // 8️⃣ Revoke old session
            await kernel.RevokeSessionAsync(
                tenantId,
                currentSessionId,
                context.Now);

            // 9️⃣ Activate new session in chain
            var updatedChain = chain.ActivateSession(
                issuedSession.Session.SessionId);

            await kernel.SaveChainAsync(
                tenantId,
                updatedChain);

            // 🔟 Root persistence (idempotent)
            await kernel.SaveSessionRootAsync(
                tenantId,
                root);

            return issuedSession;
        }

        public Task<ISession<TUserId>?> GetSessionAsync(
            string? tenantId,
            AuthSessionId sessionId)
        {
            var kernel = _factory.Create<TUserId>(tenantId);
            return kernel.GetSessionAsync(tenantId, sessionId);
        }

        public async Task RevokeSessionAsync(
            string? tenantId,
            AuthSessionId sessionId,
            DateTime at)
        {
            var kernel = _factory.Create<TUserId>(tenantId);
            await kernel.RevokeSessionAsync(tenantId, sessionId, at);
        }

        public async Task RevokeAllSessionsAsync(
            string? tenantId,
            TUserId userId,
            DateTime at)
        {
            var kernel = _factory.Create<TUserId>(tenantId);
            await kernel.RevokeSessionRootAsync(tenantId, userId, at);
        }

        public async Task RevokeChainAsync(
            string? tenantId,
            ChainId chainId,
            DateTime at)
        {
            var kernel = _factory.Create<TUserId>(tenantId);
            await kernel.RevokeChainAsync(tenantId, chainId, at);
        }
    }
}
