using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Sessions.InMemory;
using Microsoft.Extensions.Options;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public sealed class DeviceLimitAdmissionTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    private static readonly TenantKey Tenant = TenantKey.FromExternal("device-limit-tests");

    private static readonly UAuthSessionOptions SessionOptions = new()
    {
        Lifetime = TimeSpan.FromHours(8),
        MaxSessionsPerChain = 20
    };

    [Fact]
    public async Task RejectNewLogin_WhenGlobalLimitReached_KeepsExistingSessionActive()
    {
        var factory = new InMemorySessionStoreFactory();
        var issuer = CreateIssuer(factory, maxDevices: 1);
        var user = UserKey.New();
        var store = factory.Create(Tenant);

        var first = await issuer.IssueSessionAsync(
            Context(user, Device("first-device-00000000000000000001", "web")), TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<UAuthConflictException>(() =>
            issuer.IssueSessionAsync(
                Context(user, Device("second-device-00000000000000000002", "web"), Now.AddMinutes(1)), TestContext.Current.CancellationToken));

        var stored = await store.GetSessionAsync(first.Session.SessionId, TestContext.Current.CancellationToken);
        Assert.NotNull(stored);
        Assert.False(stored.IsRevoked);

        var chains = await store.GetChainsByUserAsync(user, ct: TestContext.Current.CancellationToken);
        Assert.Single(chains);
        Assert.False(chains[0].IsRevoked);
    }

    [Fact]
    public async Task RevokeOldest_WhenGlobalLimitReached_RevokesOldChainAndAllItsSessions()
    {
        var factory = new InMemorySessionStoreFactory();
        var issuer = CreateIssuer(factory, maxDevices: 1,
            behavior: DeviceLimitBehavior.RevokeOldest);
        var user = UserKey.New();
        var store = factory.Create(Tenant);

        var first = await issuer.IssueSessionAsync(
            Context(user, Device("first-device-00000000000000000001", "web")), TestContext.Current.CancellationToken);
        var second = await issuer.IssueSessionAsync(
            Context(user, Device("second-device-00000000000000000002", "web"), Now.AddMinutes(1)), TestContext.Current.CancellationToken);

        Assert.True((await store.GetSessionAsync(first.Session.SessionId, TestContext.Current.CancellationToken))!.IsRevoked);
        Assert.False((await store.GetSessionAsync(second.Session.SessionId, TestContext.Current.CancellationToken))!.IsRevoked);

        var chains = await store.GetChainsByUserAsync(user, ct: TestContext.Current.CancellationToken);
        Assert.Equal(2, chains.Count);
        Assert.Single(chains, x => !x.IsRevoked);
        Assert.True((await store.GetChainAsync(first.Session.ChainId, TestContext.Current.CancellationToken))!.IsRevoked);
        Assert.False((await store.GetChainAsync(second.Session.ChainId, TestContext.Current.CancellationToken))!.IsRevoked);
    }

    [Fact]
    public async Task SameDevice_WhenAtGlobalLimit_DoesNotConsumeAdditionalSlot()
    {
        var factory = new InMemorySessionStoreFactory();
        var issuer = CreateIssuer(factory, maxDevices: 1);
        var user = UserKey.New();
        var device = Device("same-device-00000000000000000001", "web");

        var first = await issuer.IssueSessionAsync(Context(user, device), TestContext.Current.CancellationToken);
        var second = await issuer.IssueSessionAsync(
            Context(user, device, Now.AddMinutes(1)), TestContext.Current.CancellationToken);

        Assert.NotEqual(first.Session.ChainId, second.Session.ChainId);
        Assert.False((await factory.Create(Tenant)
            .GetSessionAsync(first.Session.SessionId, TestContext.Current.CancellationToken))!.IsRevoked);
        Assert.False((await factory.Create(Tenant)
            .GetSessionAsync(second.Session.SessionId, TestContext.Current.CancellationToken))!.IsRevoked);

        await Assert.ThrowsAsync<UAuthConflictException>(() =>
            issuer.IssueSessionAsync(Context(user,
                Device("other-device-00000000000000000002", "web"),
                Now.AddMinutes(2)), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task PerTypeLimit_RejectsOnlyMatchingType()
    {
        var factory = new InMemorySessionStoreFactory();
        var issuer = CreateIssuer(factory, configure: limits =>
            limits.TypeLimits["mobile"] = 1);
        var user = UserKey.New();

        var mobile = await issuer.IssueSessionAsync(
            Context(user, Device("mobile-device-0000000000000000001", "mobile")), TestContext.Current.CancellationToken);
        var web = await issuer.IssueSessionAsync(
            Context(user, Device("web-device-000000000000000000001", "web"),
                Now.AddMinutes(1)), TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<UAuthConflictException>(() =>
            issuer.IssueSessionAsync(Context(user,
                Device("mobile-device-0000000000000000002", "mobile"),
                Now.AddMinutes(2)), TestContext.Current.CancellationToken));

        var store = factory.Create(Tenant);
        Assert.False((await store.GetSessionAsync(mobile.Session.SessionId, TestContext.Current.CancellationToken))!.IsRevoked);
        Assert.False((await store.GetSessionAsync(web.Session.SessionId, TestContext.Current.CancellationToken))!.IsRevoked);
    }

    [Fact]
    public async Task PerTypeRevokeOldest_DoesNotRevokeOtherType()
    {
        var factory = new InMemorySessionStoreFactory();
        var issuer = CreateIssuer(factory,
            behavior: DeviceLimitBehavior.RevokeOldest,
            configure: limits => limits.TypeLimits["mobile"] = 1);
        var user = UserKey.New();

        var oldMobile = await issuer.IssueSessionAsync(
            Context(user, Device("mobile-device-0000000000000000001", "mobile")), TestContext.Current.CancellationToken);
        var web = await issuer.IssueSessionAsync(
            Context(user, Device("web-device-000000000000000000001", "web"),
                Now.AddMinutes(1)), TestContext.Current.CancellationToken);
        var newMobile = await issuer.IssueSessionAsync(
            Context(user, Device("mobile-device-0000000000000000002", "mobile"),
                Now.AddMinutes(2)), TestContext.Current.CancellationToken);

        var store = factory.Create(Tenant);
        Assert.True((await store.GetSessionAsync(oldMobile.Session.SessionId, TestContext.Current.CancellationToken))!.IsRevoked);
        Assert.False((await store.GetSessionAsync(web.Session.SessionId, TestContext.Current.CancellationToken))!.IsRevoked);
        Assert.False((await store.GetSessionAsync(newMobile.Session.SessionId, TestContext.Current.CancellationToken))!.IsRevoked);
    }

    [Fact]
    public async Task RepeatedDevice_PreservesOriginalTypeInChainAndSession()
    {
        var factory = new InMemorySessionStoreFactory();
        var issuer = CreateIssuer(factory, maxDevices: 2,
            configure: limits => limits.TypeLimits["mobile"] = 1);
        var user = UserKey.New();
        const string id = "same-device-00000000000000000001";

        await issuer.IssueSessionAsync(Context(user, Device(id, "web")), TestContext.Current.CancellationToken);
        var repeated = await issuer.IssueSessionAsync(
            Context(user, Device(id, "mobile"), Now.AddMinutes(1)), TestContext.Current.CancellationToken);

        var repeatedChain = await factory.Create(Tenant)
            .GetChainAsync(repeated.Session.ChainId, TestContext.Current.CancellationToken);

        Assert.NotNull(repeatedChain);
        Assert.Equal("web", repeatedChain.Device.DeviceType);
        Assert.Equal("web", repeated.Session.Device.DeviceType);

        // A genuinely new mobile device must still have its mobile slot.
        var mobile = await issuer.IssueSessionAsync(
            Context(user, Device("mobile-device-0000000000000000002", "mobile"),
                Now.AddMinutes(2)), TestContext.Current.CancellationToken);
        Assert.Equal("mobile", mobile.Session.Device.DeviceType);
    }

    [Fact]
    public async Task SuppliedChainId_WithDifferentDeviceId_IsRejected()
    {
        var factory = new InMemorySessionStoreFactory();
        var issuer = CreateIssuer(factory, maxDevices: 2);
        var user = UserKey.New();

        var first = await issuer.IssueSessionAsync(
            Context(user, Device("first-device-00000000000000000001", "web")), TestContext.Current.CancellationToken);

        await Assert.ThrowsAsync<UAuthValidationException>(() =>
            issuer.IssueSessionAsync(Context(
                user,
                Device("second-device-00000000000000000002", "web"),
                Now.AddMinutes(1),
                first.Session.ChainId), TestContext.Current.CancellationToken));

        Assert.False((await factory.Create(Tenant)
            .GetSessionAsync(first.Session.SessionId, TestContext.Current.CancellationToken))!.IsRevoked);
        Assert.Single(await factory.Create(Tenant).GetChainsByUserAsync(user, ct: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task ConcurrentRejectNewLogin_OnlyOneAdmissionSucceeds()
    {
        var factory = new InMemorySessionStoreFactory();
        var issuer = CreateIssuer(factory, maxDevices: 1);
        var user = UserKey.New();

        async Task<bool> TryIssueAsync(string deviceId)
        {
            try
            {
                await issuer.IssueSessionAsync(Context(user, Device(deviceId, "web")));
                return true;
            }
            catch (UAuthConflictException)
            {
                return false;
            }
        }

        var outcomes = await Task.WhenAll(
            TryIssueAsync("first-device-00000000000000000001"),
            TryIssueAsync("second-device-00000000000000000002"));

        Assert.Single(outcomes, x => x);

        var chains = await factory.Create(Tenant).GetChainsByUserAsync(user, ct: TestContext.Current.CancellationToken);
        Assert.Single(chains, x => !x.IsRevoked);
    }

    [Fact]
    public async Task ConcurrentRevokeOldest_NeverLeavesMoreThanOneActiveDevice()
    {
        var factory = new InMemorySessionStoreFactory();
        var issuer = CreateIssuer(factory, maxDevices: 1,
            behavior: DeviceLimitBehavior.RevokeOldest);
        var user = UserKey.New();

        var results = await Task.WhenAll(
            issuer.IssueSessionAsync(
                Context(user, Device("first-device-00000000000000000001", "web")), TestContext.Current.CancellationToken),
            issuer.IssueSessionAsync(
                Context(user, Device("second-device-00000000000000000002", "web")), TestContext.Current.CancellationToken));

        Assert.Equal(2, results.Length);
        var chains = await factory.Create(Tenant).GetChainsByUserAsync(user, ct: TestContext.Current.CancellationToken);
        Assert.Single(chains, x => !x.IsRevoked);
        var sessions = await Task.WhenAll(results.Select(
            x => factory.Create(Tenant).GetSessionAsync(x.Session.SessionId, TestContext.Current.CancellationToken)));
        Assert.Single(sessions, x => x is { IsRevoked: false });
    }

    [Fact]
    public async Task RevokeOldest_WhenCreatingNewSessionFails_RestoresOldDevice()
    {
        var factory = new InMemorySessionStoreFactory();
        const string reusedToken = "opaque-session-token-000000000000000000000001";
        var issuer = CreateIssuer(factory, maxDevices: 1,
            behavior: DeviceLimitBehavior.RevokeOldest,
            generateToken: () => reusedToken);
        var user = UserKey.New();

        var first = await issuer.IssueSessionAsync(
            Context(user, Device("first-device-00000000000000000001", "web")), TestContext.Current.CancellationToken);

        // The new chain and revocation are staged, but CreateSessionAsync
        // fails because the issued session ID already exists.
        await Assert.ThrowsAsync<UAuthConcurrencyException>(() =>
            issuer.IssueSessionAsync(Context(user,
                Device("second-device-00000000000000000002", "web"),
                Now.AddMinutes(1)), TestContext.Current.CancellationToken));

        var store = factory.Create(Tenant);
        Assert.False((await store.GetSessionAsync(first.Session.SessionId, TestContext.Current.CancellationToken))!.IsRevoked);
        var chains = await store.GetChainsByUserAsync(user, ct: TestContext.Current.CancellationToken);
        Assert.Single(chains);
        Assert.False(chains[0].IsRevoked);
    }

    [Fact]
    public async Task DifferentUsers_HaveIndependentDeviceLimits()
    {
        var factory = new InMemorySessionStoreFactory();
        var issuer = CreateIssuer(factory, maxDevices: 1);
        var userA = UserKey.New();
        var userB = UserKey.New();

        await issuer.IssueSessionAsync(
            Context(userA, Device("first-device-00000000000000000001", "web")), TestContext.Current.CancellationToken);
        await issuer.IssueSessionAsync(
            Context(userB, Device("second-device-00000000000000000002", "web")), TestContext.Current.CancellationToken);

        Assert.Single(await factory.Create(Tenant).GetChainsByUserAsync(userA, ct: TestContext.Current.CancellationToken));
        Assert.Single(await factory.Create(Tenant).GetChainsByUserAsync(userB, ct: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DeviceLimitRejection_PreservesErrorCodeContract()
    {
        var factory = new InMemorySessionStoreFactory();
        var issuer = CreateIssuer(factory, maxDevices: 1);
        var user = UserKey.New();
        var ct = TestContext.Current.CancellationToken;

        await issuer.IssueSessionAsync(
            Context(
                user,
                Device("first-device-00000000000000000001", "web")),
            ct);

        var exception = await Assert.ThrowsAsync<UAuthConflictException>(
            () => issuer.IssueSessionAsync(
                Context(
                    user,
                    Device("second-device-00000000000000000002", "web"),
                    Now.AddMinutes(1)),
                ct));

        Assert.Equal("device_limit_exceeded", exception.Code);
    }

    [Fact]
    public async Task ReusingLoggedOutChain_MustNotBypassDeviceLimit()
    {
        var factory = new InMemorySessionStoreFactory();

        var issuer = CreateIssuer(
            factory,
            maxDevices: 2,
            configure: limits => limits.TypeLimits["web"] = 2);

        var user = UserKey.New();
        var store = factory.Create(Tenant);
        var ct = TestContext.Current.CancellationToken;

        var chrome = Device(
            "chrome-device-000000000000000001", "web");

        var firefox = Device(
            "firefox-device-00000000000000001", "web");

        var edge = Device(
            "edge-device-0000000000000000001", "web");

        // Chrome and Firefox occupy both available slots.
        var chromeSession = await issuer.IssueSessionAsync(
            Context(user, chrome),
            ct);

        var firefoxSession = await issuer.IssueSessionAsync(
            Context(user, firefox, Now.AddMinutes(1)),
            ct);

        // Firefox logs out, releasing its active slot.
        await store.LogoutChainAsync(
            firefoxSession.Session.ChainId,
            Now.AddMinutes(2),
            ct);

        // Edge occupies the released slot.
        var edgeSession = await issuer.IssueSessionAsync(
            Context(user, edge, Now.AddMinutes(3)),
            ct);

        // Firefox must not bypass admission by reusing its old chain.
        await Assert.ThrowsAsync<UAuthConflictException>(
            () => issuer.IssueSessionAsync(
                Context(
                    user,
                    firefox,
                    Now.AddMinutes(4),
                    firefoxSession.Session.ChainId),
                ct));

        // Existing sessions must remain valid.
        var storedChrome = await store.GetSessionAsync(
            chromeSession.Session.SessionId,
            ct);

        var storedEdge = await store.GetSessionAsync(
            edgeSession.Session.SessionId,
            ct);

        Assert.NotNull(storedChrome);
        Assert.NotNull(storedEdge);

        Assert.False(storedChrome.IsRevoked);
        Assert.False(storedEdge.IsRevoked);

        // Exactly two active device chains remain.
        var chains = await store.GetChainsByUserAsync(user, ct: ct);

        Assert.Equal(
            2,
            chains.Count(x => x.IsActive && !x.IsRevoked));
    }

    private static UAuthSessionIssuer CreateIssuer(
        ISessionStoreFactory factory,
        int? maxDevices = null,
        DeviceLimitBehavior behavior = DeviceLimitBehavior.RejectNewLogin,
        Action<UAuthDeviceLimitOptions>? configure = null,
        Func<string>? generateToken = null)
    {
        var options = new UAuthServerOptions();
        options.DeviceLimits.MaxDevices = maxDevices;
        options.DeviceLimits.Behavior = behavior;
        configure?.Invoke(options.DeviceLimits);

        var generator = new Mock<IOpaqueTokenGenerator>();
        generator.Setup(x => x.Generate()).Returns(
            generateToken ?? (() => "opaque-session-token-" + Guid.NewGuid().ToString("N")));

        return new UAuthSessionIssuer(
            factory,
            generator.Object,
            Options.Create(options));
    }

    private static SessionIssuanceContext Context(
        UserKey user, DeviceContext device,
        DateTimeOffset? now = null,
        SessionChainId? chainId = null) => new()
        {
            Tenant = Tenant,
            UserKey = user,
            Device = device,
            Now = now ?? Now,
            Claims = ClaimsSnapshot.Empty,
            Metadata = SessionMetadata.Empty,
            Mode = UAuthMode.Hybrid,
            SessionOptions = SessionOptions,
            ChainId = chainId
        };

    private static DeviceContext Device(string id, string type) =>
        DeviceContext.Create(
            DeviceId.Create(id),
            deviceType: type,
            platform: "web",
            operatingSystem: "test-os",
            browser: "test-browser",
            ipAddress: "127.0.0.1");
}

public sealed class InMemorySessionAdmissionTransactionTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 11, 15, 12, 0, 0, TimeSpan.Zero);

    private static readonly TenantKey Tenant =
        TenantKey.FromExternal("admission-transaction-tests");

    [Fact]
    public async Task FailedAdmission_DoesNotPublishNewRoot()
    {
        var store = new InMemorySessionStoreFactory().Create(Tenant);
        var user = UserKey.New();
        var root = UAuthSessionRoot.Create(Tenant, user, Now);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.ExecuteAdmissionAsync<int>(async ct =>
            {
                await store.CreateRootAsync(root, ct);
                throw new InvalidOperationException("simulated admission failure");
            }, TestContext.Current.CancellationToken));

        Assert.Null(await store.GetActiveRootByUserAsync(user, ct: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task InProgressAdmission_IsNotVisibleToOutsideReaders()
    {
        var store = new InMemorySessionStoreFactory().Create(Tenant);
        var user = UserKey.New();
        var root = UAuthSessionRoot.Create(Tenant, user, Now);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var entered = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        var admission = store.ExecuteAdmissionAsync(async ct =>
        {
            await store.CreateRootAsync(root, ct);
            entered.TrySetResult(true);
            await release.Task.WaitAsync(ct);
            return true;
        }, timeout.Token);

        try
        {
            await entered.Task.WaitAsync(timeout.Token);
            // The reader is outside the transaction's async execution flow.
            Assert.Null(await store.GetActiveRootByUserAsync(user, ct: TestContext.Current.CancellationToken));
        }
        finally
        {
            release.TrySetResult(true);
        }

        Assert.True(await admission);
        Assert.NotNull(await store.GetActiveRootByUserAsync(user, ct: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task DirectWriteDuringAdmission_IsPreservedAndCausesCommitConflict()
    {
        var store = new InMemorySessionStoreFactory().Create(Tenant);
        var stagedUser = UserKey.New();
        var externalUser = UserKey.New();
        var stagedRoot = UAuthSessionRoot.Create(Tenant, stagedUser, Now);
        var externalRoot = UAuthSessionRoot.Create(Tenant, externalUser, Now);

        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        var entered = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var release = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        var admission = store.ExecuteAdmissionAsync(async ct =>
        {
            await store.CreateRootAsync(stagedRoot, ct);
            entered.TrySetResult(true);
            await release.Task.WaitAsync(ct);
            return true;
        }, timeout.Token);

        try
        {
            await entered.Task.WaitAsync(timeout.Token);
            await store.CreateRootAsync(externalRoot, timeout.Token);
        }
        finally
        {
            release.TrySetResult(true);
        }

        await Assert.ThrowsAsync<UAuthConcurrencyException>(async () =>
        {
            await admission;
        });

        Assert.Null(await store.GetActiveRootByUserAsync(stagedUser, ct: TestContext.Current.CancellationToken));
        Assert.NotNull(await store.GetActiveRootByUserAsync(externalUser, ct: TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task CancelledAdmission_DoesNotPublishChanges()
    {
        var store = new InMemorySessionStoreFactory().Create(Tenant);
        var user = UserKey.New();
        var root = UAuthSessionRoot.Create(Tenant, user, Now);
        using var cancellation = new CancellationTokenSource();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await store.ExecuteAdmissionAsync(async ct =>
            {
                await store.CreateRootAsync(root, ct);
                cancellation.Cancel();
                return true;
            }, cancellation.Token);
        });

        Assert.Null(await store.GetActiveRootByUserAsync(user, ct: TestContext.Current.CancellationToken));
    }
}
