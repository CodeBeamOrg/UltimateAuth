using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Models;

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

        public async Task<SessionResult<TUserId>> CreateLoginSessionAsync(TUserId userId, DeviceInfo device, SessionMetadata? metadata, DateTime now)
        {
            var root = await _store.GetSessionRootAsync(userId) ?? UAuthSessionRoot<TUserId>.CreateNew(userId, now);

            var session = UAuthSession<TUserId>.CreateNew(
                userId,
                root.SecurityVersion,
                device,
                metadata ?? new SessionMetadata(),
                now,
                _options.SessionLifetime
            );

            var chain = UAuthSessionChain<TUserId>.CreateNew(
                userId,
                root.SecurityVersion,
                session,
                claimsSnapshot: null
            );

            // 4) Root'a chain ekle
            var updatedRoot = root.AddChain(chain, now);

            // 5) Store’da sakla
            await _store.SaveSessionAsync(session);
            await _store.SaveChainAsync(chain);
            await _store.SaveSessionRootAsync(updatedRoot);

            // 6) Active session ID set et
            await _store.SetActiveSessionIdAsync(chain.ChainId, session.SessionId);

            return new SessionResult<TUserId>
            {
                Session = session,
                Chain = chain,
                Root = updatedRoot
            };
        }

        public async Task<SessionResult<TUserId>> RefreshSessionAsync(AuthSessionId currentSessionId, DateTime now)
        {
            var oldSession = await _store.GetSessionAsync(currentSessionId) ?? throw new InvalidOperationException("Session not found");

            var chain = await _store.GetChainAsync(chainId: FindChainIdFromSession(oldSession))
                        ?? throw new InvalidOperationException("Chain not found");

            var root = await _store.GetSessionRootAsync(oldSession.UserId)
                       ?? throw new InvalidOperationException("Session root missing");

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
                _options.SessionLifetime
            );

            var rotatedChain = chain.AddRotatedSession(newSession);

            await _store.SaveSessionAsync(newSession);
            await _store.UpdateChainAsync(rotatedChain);

            await _store.SetActiveSessionIdAsync(chain.ChainId, newSession.SessionId);

            return new SessionResult<TUserId>
            {
                Session = newSession,
                Chain = rotatedChain,
                Root = root
            };
        }

        private ChainId FindChainIdFromSession(ISession<TUserId> session)
        {
            // Uygulamalar chain id'yi session metadata'ya koyabilir.
            // Default implementation bunu store üzerinden lookup ile çözer.
            throw new NotImplementedException("Store should provide reverse lookup.");
        }

        public async Task RevokeSessionAsync(AuthSessionId sessionId, DateTime at)
        {
            await _store.RevokeSessionAsync(sessionId, at);
        }

        public async Task RevokeChainAsync(ChainId chainId, DateTime at)
        {
            await _store.RevokeChainAsync(chainId, at);
        }

        public async Task RevokeRootAsync(TUserId userId, DateTime at)
        {
            await _store.RevokeSessionRootAsync(userId, at);
        }

        public async Task<SessionValidationResult<TUserId>> ValidateSessionAsync(AuthSessionId sessionId, DateTime now)
        {
            var session = await _store.GetSessionAsync(sessionId);

            if (session == null)
            {
                return new SessionValidationResult<TUserId>
                {
                    State = SessionState.Expired
                };
            }

            var chain = await _store.GetChainAsync(FindChainIdFromSession(session));
            var root = await _store.GetSessionRootAsync(session.UserId);

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

        // ------------------------------------------------------------
        // LOOKUPS
        // ------------------------------------------------------------
        public Task<IReadOnlyList<ISessionChain<TUserId>>> GetChainsAsync(TUserId userId)
            => _store.GetChainsByUserAsync(userId);

        public Task<IReadOnlyList<ISession<TUserId>>> GetSessionsAsync(ChainId chainId)
            => _store.GetSessionsByChainAsync(chainId);
    }
}
