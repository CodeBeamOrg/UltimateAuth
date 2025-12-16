using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Server.Issuers;
using CodeBeam.UltimateAuth.Server.Options;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
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

        public async Task<IssuedSession<TUserId>> CreateLoginSessionAsync(AuthenticatedSessionContext<TUserId> context)
        {
            var kernel = _factory.Create<TUserId>(context.TenantId);

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
                throw new UAuthSessionRootRevokedException(context.UserId!);
            }

            ISessionChain<TUserId> chain;

            if (context.ChainId is not null)
            {
                chain = await kernel.GetChainAsync(
                    context.TenantId,
                    context.ChainId.Value)
                    ?? throw new UAuthSessionChainNotFoundException(
                        context.ChainId.Value);

                if (chain.IsRevoked)
                    throw new UAuthSessionChainRevokedException(
                        chain.ChainId);
            }
            else
            {
                chain = UAuthSessionChain<TUserId>.Create(
                    ChainId.New(),
                    context.TenantId,
                    context.UserId,
                    root.SecurityVersion,
                    context.Claims);
            }

            var issuedSession = await _sessionIssuer.IssueAsync(
                context,
                chain);

            await kernel.ExecuteAsync(async () =>
            {
                await kernel.SaveSessionAsync(
                    context.TenantId,
                    issuedSession.Session);

                var updatedChain = chain.AttachSession(
                    issuedSession.Session.SessionId);

                await kernel.SaveChainAsync(
                    context.TenantId,
                    updatedChain);

                await kernel.SaveSessionRootAsync(
                    context.TenantId,
                    root);
            });

            return issuedSession;
        }

        public async Task<IssuedSession<TUserId>> RotateSessionAsync(SessionRotationContext<TUserId> context)
        {
            var kernel = _factory.Create<TUserId>(context.TenantId);

            var currentSession = await kernel.GetSessionAsync(
                context.TenantId,
                context.CurrentSessionId);

            if (currentSession is null)
                throw new UAuthSessionNotFoundException(context.CurrentSessionId);

            if (currentSession.IsRevoked)
                throw new UAuthSessionRevokedException(context.CurrentSessionId);

            var state = currentSession.GetState(context.Now);
            if (state != SessionState.Active)
                throw new UAuthSessionInvalidStateException(
                    context.CurrentSessionId, state);

            var chainId = await kernel.GetChainIdBySessionAsync(
                context.TenantId,
                context.CurrentSessionId);

            if (chainId is null)
                throw new UAuthSessionChainLinkMissingException(context.CurrentSessionId);

            var chain = await kernel.GetChainAsync(
                context.TenantId,
                chainId.Value);

            if (chain is null || chain.IsRevoked)
                throw new UAuthSessionChainRevokedException(chainId.Value);

            var root = await kernel.GetSessionRootAsync(
                context.TenantId,
                currentSession.UserId);

            if (root is null || root.IsRevoked)
                throw new UAuthSessionRootRevokedException(
                    currentSession.UserId!);

            if (currentSession.SecurityVersionAtCreation != root.SecurityVersion)
                throw new UAuthSessionSecurityMismatchException(
                    context.CurrentSessionId,
                    root.SecurityVersion);

            var issueContext = new AuthenticatedSessionContext<TUserId>
            {
                TenantId = root.TenantId,
                UserId = currentSession.UserId,
                Now = context.Now,
                DeviceInfo = context.Device,
                Claims = context.Claims
            };

            var issuedSession = await _sessionIssuer.IssueAsync(
                issueContext,
                chain);

            await kernel.ExecuteAsync(async () =>
            {
                await kernel.RevokeSessionAsync(
                    context.TenantId,
                    context.CurrentSessionId,
                    context.Now);

                await kernel.SaveSessionAsync(
                    context.TenantId,
                    issuedSession.Session);

                var rotatedChain = chain.RotateSession(
                    issuedSession.Session.SessionId);

                await kernel.SaveChainAsync(
                    context.TenantId,
                    rotatedChain);

                await kernel.SaveSessionRootAsync(
                    context.TenantId,
                    root);
            });

            return issuedSession;
        }

        public async Task<SessionValidationResult<TUserId>> ValidateSessionAsync(
            SessionValidationContext context)
        {
            var kernel = _factory.Create<TUserId>(context.TenantId);

            // 1️⃣ Load session
            var session = await kernel.GetSessionAsync(
                context.TenantId,
                context.SessionId);

            if (session is null)
                return SessionValidationResult<TUserId>.Invalid(SessionState.NotFound);

            var state = session.GetState(context.Now);

            if (state != SessionState.Active)
                return SessionValidationResult<TUserId>.Invalid(state);

            // 2️⃣ Resolve chain
            var chainId = await kernel.GetChainIdBySessionAsync(
                context.TenantId,
                context.SessionId);

            if (chainId is null)
                return SessionValidationResult<TUserId>.Invalid(SessionState.Invalid);

            var chain = await kernel.GetChainAsync(
                context.TenantId,
                chainId.Value);

            if (chain is null || chain.IsRevoked)
                return SessionValidationResult<TUserId>.Invalid(SessionState.Revoked);

            // 3️⃣ Resolve root
            var root = await kernel.GetSessionRootAsync(
                context.TenantId,
                session.UserId);

            if (root is null || root.IsRevoked)
                return SessionValidationResult<TUserId>.Invalid(SessionState.Revoked);

            // 4️⃣ Security version check
            if (session.SecurityVersionAtCreation != root.SecurityVersion)
                return SessionValidationResult<TUserId>.Invalid(SessionState.SecurityMismatch);

            // 5️⃣ Device check
            if (!session.Device.Matches(context.Device))
                return SessionValidationResult<TUserId>.Invalid(SessionState.DeviceMismatch);

            // 6️⃣ Touch session (best-effort)
            if (session.ShouldUpdateLastSeen(context.Now))
            {
                var updated = session.Touch(context.Now);
                await kernel.SaveSessionAsync(context.TenantId, updated);
                session = updated;
            }

            // 7️⃣ Success
            return SessionValidationResult<TUserId>.Active(
                session,
                chain,
                root);
        }

        public Task<ISession<TUserId>?> GetSessionAsync(string? tenantId, AuthSessionId sessionId)
        {
            var kernel = _factory.Create<TUserId>(tenantId);
            return kernel.GetSessionAsync(tenantId, sessionId);
        }

        public Task<IReadOnlyList<ISession<TUserId>>> GetSessionsAsync(string? tenantId, ChainId chainId)
        {
            var kernel = _factory.Create<TUserId>(tenantId);
            return kernel.GetSessionsByChainAsync(tenantId, chainId);
        }

        public Task<IReadOnlyList<ISessionChain<TUserId>>> GetChainsAsync(string? tenantId, TUserId userId)
        {
            var kernel = _factory.Create<TUserId>(tenantId);
            return kernel.GetChainsByUserAsync(tenantId, userId);
        }

        public async Task<ChainId?> ResolveChainIdAsync(
            string? tenantId,
            AuthSessionId sessionId)
        {
            var kernel = _factory.Create<TUserId>(tenantId);
            return await kernel.GetChainIdBySessionAsync(tenantId, sessionId);
        }

        public async Task RevokeSessionAsync(string? tenantId, AuthSessionId sessionId, DateTime at)
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

        public async Task RevokeChainAsync(string? tenantId, ChainId chainId, DateTime at)
        {
            var kernel = _factory.Create<TUserId>(tenantId);
            await kernel.RevokeChainAsync(tenantId, chainId, at);
        }

        public async Task RevokeAllChainsAsync(
            string? tenantId,
            TUserId userId,
            ChainId? exceptChainId,
            DateTime at)
        {
            var kernel = _factory.Create<TUserId>(tenantId);

            var chains = await kernel.GetChainsByUserAsync(tenantId, userId);

            await kernel.ExecuteAsync(async () =>
            {
                foreach (var chain in chains)
                {
                    if (exceptChainId.HasValue &&
                        chain.ChainId.Equals(exceptChainId.Value))
                    {
                        continue;
                    }

                    if (!chain.IsRevoked)
                    {
                        await kernel.RevokeChainAsync(
                            tenantId,
                            chain.ChainId,
                            at);
                    }
                }
            });
        }

        public async Task RevokeRootAsync(string? tenantId, TUserId userId, DateTime at)
        {
            var kernel = _factory.Create<TUserId>(tenantId);

            await kernel.ExecuteAsync(async () =>
            {
                await kernel.RevokeSessionRootAsync(
                    tenantId,
                    userId,
                    at);
            });
        }

    }
}
