using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
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

    public async Task<PagedResult<SessionChainSummaryDto>> GetUserChainsAsync(AccessContext context, UserKey userKey, PageRequest request, CancellationToken ct = default)
    {
        var command = new AccessCommand<PagedResult<SessionChainSummaryDto>>(async innerCt =>
        {
            var store = _storeFactory.Create(context.ResourceTenant);
            request = request.Normalize();
            var chains = await store.GetChainsByUserAsync(userKey);
            var actorChainId = context.ActorChainId;

            if (!string.IsNullOrWhiteSpace(request.SortBy))
            {
                chains = request.SortBy switch
                {
                    nameof(SessionChainSummaryDto.ChainId) =>
                        request.Descending
                            ? chains.OrderByDescending(x => x.ChainId).ToList()
                            : chains.OrderBy(x => x.Version).ToList(),

                    nameof(SessionChainSummaryDto.CreatedAt) =>
                        request.Descending
                            ? chains.OrderByDescending(x => x.CreatedAt).ToList()
                            : chains.OrderBy(x => x.Version).ToList(),

                    nameof(SessionChainSummaryDto.DeviceType) =>
                        request.Descending
                            ? chains.OrderByDescending(x => x.Device.DeviceType).ToList()
                            : chains.OrderBy(x => x.Version).ToList(),

                    nameof(SessionChainSummaryDto.Platform) =>
                        request.Descending
                            ? chains.OrderByDescending(x => x.Device.Platform).ToList()
                            : chains.OrderBy(x => x.Version).ToList(),

                    nameof(SessionChainSummaryDto.RotationCount) =>
                        request.Descending
                            ? chains.OrderByDescending(x => x.RotationCount).ToList()
                            : chains.OrderBy(x => x.RotationCount).ToList(),

                    _ => chains
                };
            }

            var total = chains.Count;

            var pageItems = chains
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(c => new SessionChainSummaryDto
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
                    IsCurrentDevice = actorChainId.HasValue && c.ChainId == actorChainId.Value
                })
                .ToList();

            return new PagedResult<SessionChainSummaryDto>(pageItems, total, request.PageNumber, request.PageSize, request.SortBy, request.Descending);
        });

        return await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task<SessionChainDetailDto> GetUserChainDetailAsync(AccessContext context, UserKey userKey, SessionChainId chainId, CancellationToken ct = default)
    {
        var command = new AccessCommand<SessionChainDetailDto>(async innerCt =>
        {
            var store = _storeFactory.Create(context.ResourceTenant);

            var chain = await store.GetChainAsync(chainId) ?? throw new InvalidOperationException("chain_not_found");

            if (chain.UserKey != userKey)
                throw new UnauthorizedAccessException();

            var sessions = await store.GetSessionsByChainAsync(chainId);

            return new SessionChainDetailDto(
                chain.ChainId,
                null,
                null,
                DateTimeOffset.MinValue,
                null,
                chain.RotationCount,
                chain.IsRevoked,
                chain.ActiveSessionId,
                sessions.Select(s => new SessionInfoDto(s.SessionId, s.CreatedAt, s.ExpiresAt, s.IsRevoked)).ToList());
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

            await store.SaveSessionAsync(revoked, expected);
        });

        await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task RevokeUserChainAsync(AccessContext context, UserKey userKey, SessionChainId chainId, CancellationToken ct = default)
    {
        var command = new AccessCommand(async innerCt =>
        {
            var store = _storeFactory.Create(context.ResourceTenant);
            await store.RevokeChainCascadeAsync(chainId, _clock.UtcNow);
        });

        await _accessOrchestrator.ExecuteAsync(context, command, ct);
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
            var chains = await store.GetChainsByUserAsync(userKey);

            foreach (var chain in chains)
            {
                if (exceptChainId.HasValue && chain.ChainId == exceptChainId.Value)
                    continue;

                await store.RevokeChainCascadeAsync(chain.ChainId, _clock.UtcNow);
            }
        });

        await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }

    public async Task RevokeRootAsync(AccessContext context, UserKey userKey, CancellationToken ct = default)
    {
        var command = new AccessCommand(async innerCt =>
        {
            var store = _storeFactory.Create(context.ResourceTenant);
            await store.RevokeRootCascadeAsync(userKey, _clock.UtcNow);
        });

        await _accessOrchestrator.ExecuteAsync(context, command, ct);
    }
}
