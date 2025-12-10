using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Models;
using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Core.Internal
{
    internal sealed class UAuthSessionService<TUserId> : ISessionService<TUserId>
    {
        private readonly ISessionStore<TUserId> _store;
        private readonly SessionOptions _options;

        public UAuthSessionService(ISessionStore<TUserId> store, SessionOptions options)
        {
            _store = store;
            _options = options;
        }

        public async Task<SessionResult<TUserId>> CreateLoginSessionAsync(string? tenantId, TUserId userId, DeviceInfo device, SessionMetadata? metadata, DateTime now)
        {
            var root = await _store.GetSessionRootAsync(tenantId, userId) ?? UAuthSessionRoot<TUserId>.CreateNew(tenantId, userId, now);

            var session = UAuthSession<TUserId>.CreateNew(
                userId,
                root.SecurityVersion,
                device,
                metadata ?? new SessionMetadata(),
                now,
                _options.Lifetime
            );

            var chain = UAuthSessionChain<TUserId>.CreateNew(
                userId,
                root.SecurityVersion,
                session,
                claimsSnapshot: null
            );

            var concreteRoot = (UAuthSessionRoot<TUserId>)root;
            var updatedRoot = concreteRoot.AddChain(chain, now);

            await _store.SaveSessionAsync(tenantId, session);
            await _store.SaveChainAsync(tenantId, chain);
            await _store.SaveSessionRootAsync(tenantId, updatedRoot);
            await _store.SetActiveSessionIdAsync(tenantId, chain.ChainId, session.SessionId);

            return new SessionResult<TUserId>
            {
                Session = session,
                Chain = chain,
                Root = updatedRoot
            };
        }

        public async Task<SessionResult<TUserId>> RefreshSessionAsync(string? tenantId, AuthSessionId currentSessionId, DateTime now)
        {
            var oldSession = await _store.GetSessionAsync(tenantId, currentSessionId) ?? throw new InvalidOperationException("Session not found");

            var chainId = await _store.GetChainIdBySessionAsync(tenantId, currentSessionId)
                     ?? throw new InvalidOperationException("Chain not found");

            var chain = await _store.GetChainAsync(tenantId, chainId)
                       ?? throw new InvalidOperationException("Chain missing");

            var root = await _store.GetSessionRootAsync(tenantId, oldSession.UserId)
                       ?? throw new InvalidOperationException("Root missing");

            if (root.IsRevoked)
                throw new UnauthorizedAccessException("Root revoked");

            if (chain.IsRevoked)
                throw new UnauthorizedAccessException("Chain revoked");

            if (oldSession.SecurityVersionAtCreation != root.SecurityVersion)
                throw new UnauthorizedAccessException("SecurityVersion mismatch");

            if (now >= oldSession.ExpiresAt)
                throw new UnauthorizedAccessException("Session expired");

            var newSession = UAuthSession<TUserId>.CreateNew(
                oldSession.UserId,
                root.SecurityVersion,
                oldSession.Device,
                oldSession.Metadata,
                now,
                _options.Lifetime
            );

            var concreteChain = (UAuthSessionChain<TUserId>)chain;
            var rotatedChain = concreteChain.AddRotatedSession(newSession);

            await _store.SaveSessionAsync(tenantId, newSession);
            await _store.UpdateChainAsync(tenantId, rotatedChain);
            await _store.SetActiveSessionIdAsync(tenantId, chain.ChainId, newSession.SessionId);

            return new SessionResult<TUserId>
            {
                Session = newSession,
                Chain = rotatedChain,
                Root = root
            };
        }

        public async Task RevokeSessionAsync(string? tenantId, AuthSessionId sessionId, DateTime at)
        {
            await _store.RevokeSessionAsync(tenantId, sessionId, at);
        }

        public async Task RevokeChainAsync(string? tenantId, ChainId chainId, DateTime at)
        {
            await _store.RevokeChainAsync(tenantId, chainId, at);
        }

        public async Task RevokeRootAsync(string? tenantId, TUserId userId, DateTime at)
        {
            await _store.RevokeSessionRootAsync(tenantId, userId, at);
        }

        public async Task<SessionValidationResult<TUserId>> ValidateSessionAsync(string? tenantId, AuthSessionId sessionId, DateTime now)
        {
            var session = await _store.GetSessionAsync(tenantId, sessionId);

            if (session == null)
            {
                return new SessionValidationResult<TUserId>
                {
                    State = SessionState.Expired
                };
            }

            var chainId = await _store.GetChainIdBySessionAsync(tenantId, sessionId);
            var chain = chainId == null ? null : await _store.GetChainAsync(tenantId, chainId.Value);
            var root = await _store.GetSessionRootAsync(tenantId, session.UserId);

            var state = ComputeState(session, chain, root, now);

            return new SessionValidationResult<TUserId>
            {
                Session = session,
                Chain = chain,
                Root = root,
                State = state
            };
        }

        private SessionState ComputeState(ISession<TUserId> session, ISessionChain<TUserId>? chain, ISessionRoot<TUserId>? root, DateTime now)
        {
            if (root == null || chain == null)
                return SessionState.Expired;

            if (root.IsRevoked) return SessionState.RootRevoked;
            if (chain.IsRevoked) return SessionState.ChainRevoked;

            if (session.IsRevoked) return SessionState.Revoked;
            if (now >= session.ExpiresAt) return SessionState.Expired;

            if (session.SecurityVersionAtCreation != root.SecurityVersion)
                return SessionState.SecurityVersionMismatch;

            return SessionState.Active;
        }

        public Task<IReadOnlyList<ISessionChain<TUserId>>> GetChainsAsync(string? tenantId, TUserId userId)
            => _store.GetChainsByUserAsync(tenantId, userId);

        public Task<IReadOnlyList<ISession<TUserId>>> GetSessionsAsync(string? tenantId, ChainId chainId)
            => _store.GetSessionsByChainAsync(tenantId, chainId);

    }
}
