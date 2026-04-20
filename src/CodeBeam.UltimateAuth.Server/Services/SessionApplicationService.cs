using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Server.Infrastructure;

namespace CodeBeam.UltimateAuth.Server.Services;

internal sealed class SessionApplicationService : ISessionApplicationService
{
    private readonly IAccessOrchestrator _accessOrchestrator;
    private readonly ISessionStoreFactory _storeFactory;
    private readonly IClock _clock;

    public SessionApplicationService(IAccessOrchestrator accessOrchestrator, ISessionStoreFactory storeFactory, IClock clock)
    {
        _accessOrchestrator = accessOrchestrator;
        _storeFactory = storeFactory;
        _clock = clock;
    }

    public async Task<PagedResult<SessionChainSummary>> GetUserChainsAsync(AccessContext context, UserKey userKey, PageRequest request, CancellationToken ct = default)
    {
        var command = new AccessCommand<PagedResult<SessionChainSummary>>(async innerCt =>
        {
            var store = _storeFactory.Create(context.ResourceTenant);
            request = request.Normalize();
            var chains = await store.GetChainsByUserAsync(userKey);
            var actorChainId = context.ActorChainId;

            if (!string.IsNullOrWhiteSpace(request.SortBy))
            {
                chains = request.SortBy switch
                {
                    nameof(SessionChainSummary.ChainId) => request.Descending
                        ? chains.OrderByDescending(x => x.ChainId).ToList()
                        : chains.OrderBy(x => x.Version).ToList(),

                    nameof(SessionChainSummary.CreatedAt) => request.Descending
                        ? chains.OrderByDescending(x => x.CreatedAt).ToList()
                        : chains.OrderBy(x => x.Version).ToList(),

                    nameof(SessionChainSummary.LastSeenAt) => request.Descending
                        ? chains.OrderByDescending(x => x.LastSeenAt).ToList()
                        : chains.OrderBy(x => x.LastSeenAt).ToList(),

                    nameof(SessionChainSummary.RevokedAt) => request.Descending
                        ? chains.OrderByDescending(x => x.RevokedAt).ToList()
                        : chains.OrderBy(x => x.RevokedAt).ToList(),

                    nameof(SessionChainSummary.DeviceType) => request.Descending
                        ? chains.OrderByDescending(x => x.Device.DeviceType).ToList()
                        : chains.OrderBy(x => x.Device.DeviceType).ToList(),

                    nameof(SessionChainSummary.OperatingSystem) => request.Descending
                        ? chains.OrderByDescending(x => x.Device.OperatingSystem).ToList()
                        : chains.OrderBy(x => x.Device.OperatingSystem).ToList(),

                    nameof(SessionChainSummary.Platform) => request.Descending
                        ? chains.OrderByDescending(x => x.Device.Platform).ToList()
                        : chains.OrderBy(x => x.Device.Platform).ToList(),

                    nameof(SessionChainSummary.Browser) => request.Descending
                        ? chains.OrderByDescending(x => x.Device.Browser).ToList()
                        : chains.OrderBy(x => x.Device.Browser).ToList(),

                    nameof(SessionChainSummary.RotationCount) => request.Descending
                        ? chains.OrderByDescending(x => x.RotationCount).ToList()
                        : chains.OrderBy(x => x.RotationCount).ToList(),

                    nameof(SessionChainSummary.TouchCount) => request.Descending
                        ? chains.OrderByDescending(x => x.TouchCount).ToList()
                        : chains.OrderBy(x => x.TouchCount).ToList(),

                    _ => chains
                };
            }

            var total = chains.Count;

            var pageItems = chains
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(c => new SessionChainSummary
                {
                    ChainId = c.ChainId,
                    DeviceType = c.Device.DeviceType,
                    OperatingSystem = c.Device.OperatingSystem,
                    Platform = c.Device.Platform,
                    Browser = c.Device.Browser,
                    CreatedAt = c.CreatedAt,
                    LastSeenAt = c.LastSeenAt,
                    RotationCount = c.RotationCount,
                    TouchCount = c.TouchCount,
                    IsRevoked = c.IsRevoked,
                    RevokedAt = c.RevokedAt,
                    ActiveSessionId = c.ActiveSessionId,
                    IsCurrentDevice = actorChainId.HasValue && c.ChainId == actorChainId.Value,
                    State = c.State,
                })
                .ToList();

            return new PagedResult<SessionChainSummary>(pageItems, total, request.PageNumber, request.PageSize, request.SortBy, request.Descending);
        });

        return await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task<SessionChainDetail> GetUserChainDetailAsync(AccessContext context, UserKey userKey, SessionChainId chainId, CancellationToken ct = default)
    {
        var command = new AccessCommand<SessionChainDetail>(async innerCt =>
        {
            var store = _storeFactory.Create(context.ResourceTenant);
            var chain = await store.GetChainAsync(chainId) ?? throw new UAuthNotFoundException("chain_not_found");

            if (chain.UserKey != userKey)
                throw new UAuthValidationException("User conflict.");

            var sessions = await store.GetSessionsByChainAsync(chainId);

            return new SessionChainDetail
            {
                ChainId = chain.ChainId,
                DeviceType = chain.Device.DeviceType,
                OperatingSystem = chain.Device.OperatingSystem,
                Platform = chain.Device.Platform,
                Browser = chain.Device.Browser,
                CreatedAt = chain.CreatedAt,
                LastSeenAt = chain.LastSeenAt,
                State = chain.State,
                RotationCount = chain.RotationCount,
                TouchCount = chain.TouchCount,
                IsRevoked = chain.IsRevoked,
                RevokedAt = chain.RevokedAt,
                ActiveSessionId = chain.ActiveSessionId,

                Sessions = sessions
                .OrderByDescending(x => x.CreatedAt)
                .Select(s => new SessionInfo(
                    s.SessionId,
                    s.CreatedAt,
                    s.ExpiresAt,
                    s.IsRevoked))
                .ToList()
            };
        });

        return await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task RevokeUserSessionAsync(AccessContext context, UserKey userKey, AuthSessionId sessionId, CancellationToken ct = default)
    {
        var command = new AccessCommand(async innerCt =>
        {
            var store = _storeFactory.Create(context.ResourceTenant);
            var now = _clock.UtcNow;

            var session = await store.GetSessionAsync(sessionId)
                ?? throw new InvalidOperationException("session_not_found");

            if (session.UserKey != userKey)
                throw new UnauthorizedAccessException();

            var expected = session.Version;
            var revoked = session.Revoke(now);

            await store.ExecuteAsync(async innerCt2 => {
                await store.SaveSessionAsync(revoked, expected);
            });
        });

        await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task<RevokeResult> RevokeUserChainAsync(AccessContext context, UserKey userKey, SessionChainId chainId, CancellationToken ct = default)
    {
        var command = new AccessCommand<RevokeResult>(async innerCt =>
        {
            var isCurrent = context.ActorChainId == chainId;
            var store = _storeFactory.Create(context.ResourceTenant);

            await store.ExecuteAsync(async innerCt2 => {
                await store.RevokeChainCascadeAsync(chainId, _clock.UtcNow);
            });

            return new RevokeResult
            {
                CurrentChain = isCurrent,
                RootRevoked = false
            };
        });

        return await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task RevokeOtherChainsAsync(AccessContext context, UserKey userKey, SessionChainId? currentChainId, CancellationToken ct = default)
    {
        await RevokeAllChainsAsync(context, userKey, currentChainId, ct);
    }

    public async Task RevokeAllChainsAsync(AccessContext context, UserKey userKey, SessionChainId? exceptChainId, CancellationToken ct = default)
    {
        var command = new AccessCommand(async innerCt =>
        {
            var store = _storeFactory.Create(context.ResourceTenant);

            await store.ExecuteAsync(async innerCt2 => {
                var chains = await store.GetChainsByUserAsync(userKey);

                foreach (var chain in chains)
                {
                    if (exceptChainId.HasValue && chain.ChainId == exceptChainId.Value)
                        continue;

                    await store.RevokeChainCascadeAsync(chain.ChainId, _clock.UtcNow);
                }
            });
        });

        await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task<RevokeResult> LogoutDeviceAsync(AccessContext context, SessionChainId currentChainId, CancellationToken ct = default)
    {
        var command = new AccessCommand<RevokeResult>(async innerCt =>
        {
            var isCurrent = context.ActorChainId == currentChainId;
            var store = _storeFactory.Create(context.ResourceTenant);
            var now = _clock.UtcNow;

            await store.ExecuteAsync(async innerCt2 => {
                await store.LogoutChainAsync(currentChainId, now, innerCt2);
            });

            return new RevokeResult
            {
                CurrentChain = isCurrent,
                RootRevoked = false
            };
        });

        return await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task LogoutOtherDevicesAsync(AccessContext context, UserKey userKey, SessionChainId currentChainId, CancellationToken ct = default)
    {
        var command = new AccessCommand(async innerCt =>
        {
            var store = _storeFactory.Create(context.ResourceTenant);
            var now = _clock.UtcNow;

            await store.ExecuteAsync(async innerCt2 => {
                await store.RevokeOtherSessionsAsync(userKey, currentChainId, now, innerCt2);
            });
            
        });

        await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task LogoutAllDevicesAsync(AccessContext context, UserKey userKey, CancellationToken ct = default)
    {
        var command = new AccessCommand(async innerCt =>
        {
            var store = _storeFactory.Create(context.ResourceTenant);
            var now = _clock.UtcNow;

            await store.ExecuteAsync(async innerCt2 => {
                await store.RevokeAllSessionsAsync(userKey, now, innerCt2);
            });
        });

        await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task RevokeRootAsync(AccessContext context, UserKey userKey, CancellationToken ct = default)
    {
        var command = new AccessCommand(async innerCt =>
        {
            var store = _storeFactory.Create(context.ResourceTenant);

            await store.ExecuteAsync(async innerCt2 => {
                await store.RevokeRootCascadeAsync(userKey, _clock.UtcNow);
            });
        });

        await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }
}
