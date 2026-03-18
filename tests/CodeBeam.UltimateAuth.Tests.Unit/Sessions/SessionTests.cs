using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Services;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class SessionTests
{
    [Fact]
    public async Task Login_should_cleanup_old_sessions_when_limit_exceeded()
    {
        var runtime = new TestAuthRuntime<UserKey>(configureServer: o =>
        {
            o.Session.MaxSessionsPerChain = 3;
        });

        var flow = await runtime.CreateLoginFlowAsync();
        for (int i = 0; i < 5; i++)
        {
            await runtime.LoginAsync(flow);
        }

        var store = runtime.Services.GetRequiredService<ISessionStoreFactory>().Create(TenantKey.Single);
        var chains = await store.GetChainsByUserAsync(TestUsers.User);
        var chain = chains.First();
        var sessions = await store.GetSessionsByChainAsync(chain.ChainId);

        sessions.Count.Should().BeLessThanOrEqualTo(3);
    }

    [Fact]
    public async Task Logout_device_should_revoke_sessions_but_keep_chain()
    {
        var runtime = new TestAuthRuntime<UserKey>();
        var orchestrator = runtime.GetLoginOrchestrator();
        var flow = await runtime.CreateLoginFlowAsync();

        var result = await orchestrator.LoginAsync(flow, new LoginRequest
        {
            Tenant = TenantKey.Single,
            Identifier = "user",
            Secret = "user"
        });

        var store = runtime.Services.GetRequiredService<ISessionStoreFactory>().Create(TenantKey.Single);
        var chainId = await store.GetChainIdBySessionAsync(result.SessionId!.Value);
        await store.LogoutChainAsync(chainId!.Value, runtime.Clock.UtcNow);
        var chain = await store.GetChainAsync(chainId.Value);

        chain.Should().NotBeNull();
        chain!.ActiveSessionId.Should().BeNull();

        var sessions = await store.GetSessionsByChainAsync(chainId.Value);

        sessions.All(x => x.IsRevoked).Should().BeTrue();
    }

    [Fact]
    public async Task Logout_other_devices_should_keep_current_chain()
    {
        var runtime = new TestAuthRuntime<UserKey>();

        var flow1 = await runtime.CreateLoginFlowAsync();
        var flow2 = await runtime.CreateLoginFlowAsync();

        await runtime.LoginAsync(flow1);

        await runtime.LoginAsync(flow2);

        var store = runtime.Services.GetRequiredService<ISessionStoreFactory>()
            .Create(TenantKey.Single);

        var chains = await store.GetChainsByUserAsync(TestUsers.User);

        var current = chains.First();

        await store.RevokeOtherSessionsAsync(current.UserKey, current.ChainId, runtime.Clock.UtcNow);

        var updatedChains = await store.GetChainsByUserAsync(current.UserKey);

        updatedChains.Count(x => x.State == SessionChainState.Active).Should().Be(1);
    }

    [Fact]
    public async Task Get_chain_detail_should_return_sessions()
    {
        var runtime = new TestAuthRuntime<UserKey>();

        var orchestrator = runtime.GetLoginOrchestrator();
        var flow = await runtime.CreateLoginFlowAsync();

        await orchestrator.LoginAsync(flow, new LoginRequest
        {
            Tenant = TenantKeys.Single,
            Identifier = "user",
            Secret = "user"
        });

        var service = runtime.Services.GetRequiredService<ISessionApplicationService>();
        var context = TestAccessContext.ForUser(TestUsers.User, "session.query");
        var store = runtime.Services.GetRequiredService<ISessionStoreFactory>().Create(TenantKey.Single);
        var chains = await store.GetChainsByUserAsync(TestUsers.User);
        var result = await service.GetUserChainDetailAsync(context, chains.First().UserKey, chains.First().ChainId);

        result.Should().NotBeNull();
        result.Sessions.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Revoke_chain_should_revoke_all_sessions()
    {
        var runtime = new TestAuthRuntime<UserKey>();

        var orchestrator = runtime.GetLoginOrchestrator();
        var flow = await runtime.CreateLoginFlowAsync();

        await orchestrator.LoginAsync(flow, new LoginRequest
        {
            Tenant = TenantKeys.Single,
            Identifier = "user",
            Secret = "user"
        });

        var store = runtime.Services.GetRequiredService<ISessionStoreFactory>().Create(TenantKey.Single);
        var chains = await store.GetChainsByUserAsync(TestUsers.User);
        await store.RevokeChainCascadeAsync(chains.First().ChainId, runtime.Clock.UtcNow);
        var sessions = await store.GetSessionsByChainAsync(chains.First().ChainId);

        sessions.All(x => x.IsRevoked).Should().BeTrue();
    }
}
