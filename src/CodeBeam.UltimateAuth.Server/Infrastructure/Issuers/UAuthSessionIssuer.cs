using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Options;
using System.Security;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public sealed class UAuthSessionIssuer : ISessionIssuer
{
    private readonly ISessionStoreFactory _storeFactory;
    private readonly IOpaqueTokenGenerator _opaqueGenerator;
    private readonly UAuthDeviceLimitOptions _deviceLimits;

    public UAuthSessionIssuer(
        ISessionStoreFactory storeFactory,
        IOpaqueTokenGenerator opaqueGenerator,
        IOptions<UAuthServerOptions> options)
    {
        _storeFactory = storeFactory;
        _opaqueGenerator = opaqueGenerator;
        _deviceLimits = options.Value.DeviceLimits.Clone();
    }

    public async Task<IssuedSession> IssueSessionAsync(SessionIssuanceContext context, CancellationToken ct = default)
    {
        // Defensive guard — enforcement belongs to Authority
        if (context.Mode == UAuthMode.PureJwt)
            throw new InvalidOperationException("Session issuance is not allowed in PureJwt mode.");

        var sessionOptions = context.SessionOptions;
        var now = context.Now;
        var opaqueSessionId = _opaqueGenerator.Generate();

        if (!AuthSessionId.TryCreate(opaqueSessionId, out AuthSessionId sessionId))
            throw new InvalidCastException("Can't create opaque id.");

        var absoluteExpiresAt = sessionOptions.MaxLifetime is { } max
            ? now.Add(max)
            : (DateTimeOffset?)null;

        var sessionExpiresAt = now.Add(sessionOptions.Lifetime);
        if (absoluteExpiresAt is { } absolute && sessionExpiresAt > absolute)
            sessionExpiresAt = absolute;

        var kernel = _storeFactory.Create(context.Tenant);
        var enforceDeviceLimits = _deviceLimits.HasLimits;

        async Task<IssuedSession> IssueCoreAsync(CancellationToken innerCt)
        {
            var root = await kernel.GetActiveRootByUserAsync(context.UserKey, innerCt);
            if (root is null)
            {
                root = UAuthSessionRoot.Create(context.Tenant, context.UserKey, now);
                await kernel.CreateRootAsync(root, innerCt);
            }

            async Task<UAuthSessionChain> CreateNewChainAsync()
            {
                if (enforceDeviceLimits)
                {
                    var activeChains = await GetActiveDeviceChainsAsync(
                        kernel,
                        root.RootId,
                        context.UserKey,
                        now,
                        sessionOptions.IdleTimeout,
                        innerCt);

                    var chainsToRevoke = EvaluateDeviceLimits(_deviceLimits, activeChains, context.Device);

                    foreach (var chainId in chainsToRevoke)
                        await kernel.RevokeChainCascadeAsync(chainId, now, innerCt);
                }

                var created = UAuthSessionChain.Create(
                    SessionChainId.New(),
                    root.RootId,
                    context.Tenant,
                    context.UserKey,
                    now,
                    absoluteExpiresAt,
                    context.Device,
                    ClaimsSnapshot.Empty,
                    root.SecurityVersion);

                await kernel.CreateChainAsync(created, innerCt);
                return created;
            }

            UAuthSessionChain chain;
            if (context.ChainId is not null)
            {
                var existing = await kernel.GetChainAsync(context.ChainId.Value, innerCt);

                // TODO(v0.x): Re-evaluate explicit ChainId semantics.
                // When the caller explicitly supplies a ChainId but that chain cannot be found,
                // the current behavior silently creates a new chain with a different ChainId.
                // Once SemiHybrid/PureJwt and the complete chain lifecycle semantics are finalized,
                // decide whether this should instead fail closed (e.g. chain-not-found/validation failure).
                // Do not change without reviewing login, refresh, reauthentication and device-chain flows.
                chain = existing ?? await CreateNewChainAsync();

                //chain = await kernel.GetChainAsync(context.ChainId.Value)
                //    ?? throw new UAuthNotFoundException("Chain not found.");

                if (chain.GetState(now, sessionOptions.IdleTimeout) != SessionState.Active)
                    throw new UAuthValidationException("Chain is not active.");

                if (chain.UserKey != context.UserKey || chain.Tenant != context.Tenant)
                    throw new UAuthValidationException("Invalid chain ownership.");

                if (chain.RootId != root.RootId)
                    throw new UAuthValidationException(
                        "Chain does not belong to the active session root.");

                // A supplied chain must not be reused with a different device identity.
                if (enforceDeviceLimits &&
                    (chain.Device.DeviceId is not DeviceId chainDeviceId ||
                     context.Device.DeviceId is not DeviceId incomingDeviceId ||
                     chainDeviceId != incomingDeviceId))
                {
                    throw new UAuthValidationException("device_id_mismatch");
                }
            }
            else
            {
                chain = await CreateNewChainAsync();
            }

            var sessions = await kernel.GetSessionsByChainAsync(chain.ChainId, innerCt);
            if (sessions.Count >= sessionOptions.MaxSessionsPerChain)
            {
                var toDelete = sessions
                    .Where(x => x.SessionId != chain.ActiveSessionId)
                    .OrderBy(x => x.CreatedAt)
                    .Take(sessions.Count - sessionOptions.MaxSessionsPerChain + 1)
                    .ToList();

                foreach (var old in toDelete)
                    await kernel.RemoveSessionAsync(old.SessionId, innerCt);
            }

            var session = UAuthSession.Create(
                sessionId: sessionId,
                tenant: context.Tenant,
                userKey: context.UserKey,
                chainId: chain.ChainId,
                now: now,
                expiresAt: sessionExpiresAt,
                securityVersion: root.SecurityVersion,
                device: context.Device,
                claims: context.Claims,
                metadata: context.Metadata);

            await kernel.CreateSessionAsync(session, innerCt);
            var updatedChain = chain.AttachSession(session.SessionId, now);
            await kernel.SaveChainAsync(updatedChain, chain.Version, innerCt);

            return new IssuedSession
            {
                Session = session,
                OpaqueSessionId = opaqueSessionId,
                IsMetadataOnly = context.Mode == UAuthMode.SemiHybrid
            };
        }

        // TODO(DeviceLimits): ExecuteAdmissionAsync must guarantee rollback and
        // serializable admission across concurrent instances, including retry.
        // Do not enable configured limits until EF Core and InMemory implementations
        // satisfy this contract.
        return enforceDeviceLimits
            ? await kernel.ExecuteAdmissionAsync(IssueCoreAsync, ct)
            : await kernel.ExecuteAsync(IssueCoreAsync, ct);
    }

    public async Task<IssuedSession> RotateSessionAsync(SessionRotationContext context, CancellationToken ct = default)
    {
        var kernel = _storeFactory.Create(context.Tenant);
        var now = context.Now;

        var sessionOptions = context.SessionOptions;

        var opaqueSessionId = _opaqueGenerator.Generate();
        if (!AuthSessionId.TryCreate(opaqueSessionId, out var newSessionId))
            throw new InvalidCastException("Can't create opaque session id.");

        var expiresAt = now.Add(sessionOptions.Lifetime);
        if (sessionOptions.MaxLifetime is not null)
        {
            var absoluteExpiry = now.Add(sessionOptions.MaxLifetime.Value);
            if (absoluteExpiry < expiresAt)
                expiresAt = absoluteExpiry;
        }

        IssuedSession? issued = null;

        await kernel.ExecuteAsync(async _ =>
        {
            var root = await kernel.GetActiveRootByUserAsync(context.UserKey);

            if (root == null)
                throw new SecurityException("Session root not found");

            var oldSession = await kernel.GetSessionAsync(context.CurrentSessionId)
                ?? throw new SecurityException("Session not found");

            if (oldSession.IsRevoked || oldSession.ExpiresAt <= now)
                throw new SecurityException("Session is not valid");

            if (oldSession.Tenant != context.Tenant || oldSession.UserKey != context.UserKey)
                throw new SecurityException("Session does not belong to the current user/tenant.");

            if (oldSession.SecurityVersionAtCreation != root.SecurityVersion)
                throw new SecurityException("Security version mismatch");

            var chain = await kernel.GetChainAsync(oldSession.ChainId)
                ?? throw new SecurityException("Chain not found");

            if (chain.IsRevoked)
                throw new SecurityException("Chain is revoked");

            if (chain.Tenant != context.Tenant || chain.UserKey != context.UserKey)
                throw new SecurityException("Chain does not belong to the current user/tenant.");

            if (chain.RootId != root.RootId)
            {
                throw new SecurityException("Chain does not belong to the active session root.");
            }

            var newSessionUnbound = UAuthSession.Create(
                sessionId: newSessionId,
                tenant: context.Tenant,
                userKey: context.UserKey,
                chainId: SessionChainId.Unassigned,
                now: now,
                expiresAt: expiresAt,
                securityVersion: root.SecurityVersion,
                device: context.Device,
                claims: context.Claims,
                metadata: context.Metadata
            );

            var newSession = newSessionUnbound.WithChain(chain.ChainId);

            issued = new IssuedSession
            {
                Session = newSession,
                OpaqueSessionId = opaqueSessionId,
                IsMetadataOnly = context.Mode == UAuthMode.SemiHybrid
            };

            await kernel.CreateSessionAsync(newSession);
            var chainExpected = chain.Version;
            var updatedChain = chain.RotateSession(newSession.SessionId, now, context.Claims);
            await kernel.SaveChainAsync(updatedChain, chainExpected);

            var expected = oldSession.Version;
            var revokedOld = oldSession.Revoke(now);
            await kernel.SaveSessionAsync(revokedOld, expected);
        }, ct);

        if (issued == null)
            throw new InvalidCastException("Can't issued session.");
        return issued;
    }

    public async Task<bool> RevokeSessionAsync(TenantKey tenant, AuthSessionId sessionId, DateTimeOffset at, CancellationToken ct = default)
    {
        var kernel = _storeFactory.Create(tenant);
        return await kernel.ExecuteAsync(_ => kernel.RevokeSessionAsync(sessionId, at), ct);
    }

    public async Task RevokeChainAsync(TenantKey tenant, SessionChainId chainId, DateTimeOffset at, CancellationToken ct = default)
    {
        var kernel = _storeFactory.Create(tenant);
        await kernel.ExecuteAsync(async _ =>
        {
            var chain = await kernel.GetChainAsync(chainId);
            if (chain is null)
                return;

            await kernel.RevokeChainCascadeAsync(chainId, at);
        }, ct);
    }

    public async Task RevokeAllChainsAsync(TenantKey tenant, UserKey userKey, SessionChainId? exceptChainId, DateTimeOffset at, CancellationToken ct = default)
    {
        var kernel = _storeFactory.Create(tenant);
        await kernel.ExecuteAsync(async _ =>
        {
            var chains = await kernel.GetChainsByUserAsync(userKey);

            foreach (var chain in chains)
            {
                if (exceptChainId.HasValue && chain.ChainId == exceptChainId.Value)
                    continue;

                await kernel.RevokeChainCascadeAsync(chain.ChainId, at);
            }
        }, ct);
    }

    public async Task RevokeRootAsync(TenantKey tenant, UserKey userKey, DateTimeOffset at, CancellationToken ct = default)
    {
        var kernel = _storeFactory.Create(tenant);

        await kernel.ExecuteAsync(
            _ => kernel.RevokeRootCascadeAsync(userKey, at),
            ct);
    }

    public async Task<SessionChainId?> GetChainIdBySessionAsync(TenantKey tenant, AuthSessionId sessionId, CancellationToken ct = default)
    {
        var kernel = _storeFactory.Create(tenant);
        return await kernel.ExecuteAsync(innerCt => kernel.GetChainIdBySessionAsync(sessionId, innerCt), ct);
    }

    public async Task<bool> LogoutChainAsync(TenantKey tenant, SessionChainId chainId, DateTimeOffset at, CancellationToken ct = default)
    {
        var kernel = _storeFactory.Create(tenant);

        return await kernel.ExecuteAsync(async innerCt =>
        {
            var chain = await kernel.GetChainAsync(chainId, innerCt);

            if (chain is null || chain.IsRevoked)
                return false;

            await kernel.LogoutChainAsync(chainId, at, innerCt);

            return true;
        }, ct);
    }

    private static IReadOnlyList<SessionChainId> EvaluateDeviceLimits(
        UAuthDeviceLimitOptions options,
        IReadOnlyList<UAuthSessionChain> activeChains,
        DeviceContext incomingDevice)
    {
        if (!options.HasLimits)
            return [];

        if (incomingDevice.DeviceId is not DeviceId incomingId)
            throw new UAuthValidationException("device_id_required");

        var devices = activeChains
            .Where(x => x.Device.DeviceId is not null)
            .GroupBy(x => x.Device.DeviceId!.Value)
            .Select(group => group
                .OrderBy(x => x.CreatedAt)
                .ThenBy(x => x.ChainId.ToString(), StringComparer.Ordinal)
                .ToArray())
            .OrderBy(group => group[0].CreatedAt)
            .ThenBy(group => group[0].ChainId.ToString(), StringComparer.Ordinal)
            .ToList();

        // A previously admitted device consumes only one slot.
        if (devices.Any(group => group[0].Device.DeviceId == incomingId))
            return [];

        static string Normalize(string? value) =>
            string.IsNullOrWhiteSpace(value)
                ? "web"
                : value.Trim().ToLowerInvariant();

        var incomingType = Normalize(incomingDevice.DeviceType);

        var typeLimit = options.TypeLimits
            .Where(x => string.Equals(
                x.Key,
                incomingType,
                StringComparison.OrdinalIgnoreCase))
            .Select(x => (int?)x.Value)
            .FirstOrDefault();

        bool GlobalExceeded() =>
            options.MaxDevices is int max &&
            devices.Count + 1 > max;

        bool TypeExceeded() =>
            typeLimit is int max &&
            devices.Count(group =>
                Normalize(group[0].Device.DeviceType) == incomingType) + 1 > max;

        if (!GlobalExceeded() && !TypeExceeded())
            return [];

        if (options.Behavior == DeviceLimitBehavior.RejectNewLogin)
            throw new UAuthConflictException("device_limit_exceeded");

        if (options.Behavior != DeviceLimitBehavior.RevokeOldest)
            throw new UAuthValidationException("invalid_device_limit_behavior");

        var chainsToRevoke = new List<SessionChainId>();

        while (GlobalExceeded() || TypeExceeded())
        {
            var victim = TypeExceeded()
                ? devices.FirstOrDefault(group =>
                    Normalize(group[0].Device.DeviceType) == incomingType)
                : devices.FirstOrDefault();

            if (victim is null)
                throw new UAuthConflictException("device_limit_exceeded");

            chainsToRevoke.AddRange(victim.Select(x => x.ChainId));
            devices.Remove(victim);
        }

        return chainsToRevoke;
    }

    private static async Task<IReadOnlyList<UAuthSessionChain>>
        GetActiveDeviceChainsAsync(
            ISessionStore store,
            SessionRootId rootId,
            UserKey userKey,
            DateTimeOffset now,
            TimeSpan? idleTimeout,
            CancellationToken ct)
    {
        var chains = await store.GetChainsByRootAsync(rootId, ct);
        var active = new List<UAuthSessionChain>();

        foreach (var chain in chains)
        {
            if (chain.UserKey != userKey ||
                chain.GetState(now, idleTimeout) != SessionState.Active ||
                chain.ActiveSessionId is not AuthSessionId sessionId)
            {
                continue;
            }

            var session = await store.GetSessionAsync(sessionId, ct);

            if (session is null ||
                session.ChainId != chain.ChainId ||
                session.UserKey != userKey ||
                session.GetState(now) != SessionState.Active ||
                session.SecurityVersionAtCreation !=
                    chain.SecurityVersionAtCreation)
            {
                continue;
            }

            active.Add(chain);
        }

        return active;
    }
}
