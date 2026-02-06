using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using CodeBeam.UltimateAuth.Users.InMemory;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Security;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class LoginOrchestratorTests
{
    [Fact]
    public async Task Successful_login_should_return_success_result()
    {
        var runtime = new TestAuthRuntime<UserKey>();
        var orchestrator = runtime.GetLoginOrchestrator();
        var flow = await runtime.CreateLoginFlowAsync();

        var result = await orchestrator.LoginAsync(flow,
            new LoginRequest
            {
                Tenant = TenantKey.Single,
                Identifier = "user",
                Secret = "user",
                Device = TestDevice.Default(),
            });

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Successful_login_should_create_session()
    {
        var runtime = new TestAuthRuntime<UserKey>();
        var orchestrator = runtime.GetLoginOrchestrator();
        var flow = await runtime.CreateLoginFlowAsync();

        var result = await orchestrator.LoginAsync(flow,
            new LoginRequest
            {
                Tenant = TenantKey.Single,
                Identifier = "user",
                Secret = "user",
                Device = TestDevice.Default(),
            });

        result.SessionId.Should().NotBeNull();
    }

    [Fact]
    public async Task First_failed_login_should_record_attempt()
    {
        var runtime = new TestAuthRuntime<UserKey>(configureCore: o =>
        {
            o.Login.MaxFailedAttempts = 3;
        });

        var orchestrator = runtime.GetLoginOrchestrator();
        var flow = await runtime.CreateLoginFlowAsync();

        await orchestrator.LoginAsync(flow,
            new LoginRequest
            {
                Tenant = TenantKey.Single,
                Identifier = "user",
                Secret = "wrong",
                Device = TestDevice.Default(),
            });

        var store = runtime.Services.GetRequiredService<InMemoryUserSecurityStore<UserKey>>();

        var state = store.GetState(TenantKey.Single, UserKey.Parse("user", null));

        state!.FailedLoginAttempts.Should().Be(1);
    }

    [Fact]
    public async Task Successful_login_should_clear_failure_state()
    {
        var runtime = new TestAuthRuntime<UserKey>(configureCore: o =>
        {
            o.Login.MaxFailedAttempts = 3;
        });

        var orchestrator = runtime.GetLoginOrchestrator();
        var flow = await runtime.CreateLoginFlowAsync();

        await orchestrator.LoginAsync(flow,
            new LoginRequest
            {
                Tenant = TenantKey.Single,
                Identifier = "user",
                Secret = "wrong",
                Device = TestDevice.Default(),
            });

        await orchestrator.LoginAsync(flow,
            new LoginRequest
            {
                Tenant = TenantKey.Single,
                Identifier = "user",
                Secret = "user", // valid password
                Device = TestDevice.Default(),
            });

        var store = runtime.Services.GetRequiredService<InMemoryUserSecurityStore<UserKey>>();

        var state = store.GetState(TenantKey.Single,UserKey.Parse("user", null));
        state.Should().BeNull();
    }

    [Fact]
    public async Task Invalid_password_should_fail_login()
    {
        var runtime = new TestAuthRuntime<UserKey>();
        var orchestrator = runtime.GetLoginOrchestrator();
        var flow = await runtime.CreateLoginFlowAsync();

        var result = await orchestrator.LoginAsync(flow,
            new LoginRequest
            {
                Tenant = TenantKey.Single,
                Identifier = "user",
                Secret = "wrong",
                Device = TestDevice.Default(),
            });

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Non_existent_user_should_fail_login_gracefully()
    {
        var runtime = new TestAuthRuntime<UserKey>();
        var orchestrator = runtime.GetLoginOrchestrator();
        var flow = await runtime.CreateLoginFlowAsync();

        var result = await orchestrator.LoginAsync(flow,
            new LoginRequest
            {
                Tenant = TenantKey.Single,
                Identifier = "ghost",
                Secret = "whatever",
                Device = TestDevice.Default(),
            });

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task MaxFailedAttempts_one_should_lock_user_on_first_fail()
    {
        var runtime = new TestAuthRuntime<UserKey>(configureServer: o =>
        {
            o.Login.MaxFailedAttempts = 1;
        });

        var orchestrator = runtime.GetLoginOrchestrator();
        var flow = await runtime.CreateLoginFlowAsync();

        await orchestrator.LoginAsync(flow,
            new LoginRequest
            {
                Tenant = TenantKey.Single,
                Identifier = "user",
                Secret = "wrong",
                Device = TestDevice.Default(),
            });

        var store = runtime.Services.GetRequiredService<InMemoryUserSecurityStore<UserKey>>();
        var state = store.GetState(TenantKey.Single, UserKey.Parse("user", null));

        state!.IsLocked.Should().BeTrue();
    }

    [Fact]
    public async Task Locked_user_should_not_login_even_with_correct_password()
    {
        var runtime = new TestAuthRuntime<UserKey>(configureServer: o =>
        {
            o.Login.MaxFailedAttempts = 1;
        });

        var orchestrator = runtime.GetLoginOrchestrator();
        var flow = await runtime.CreateLoginFlowAsync();

        // lock
        await orchestrator.LoginAsync(flow,
            new LoginRequest
            {
                Tenant = TenantKey.Single,
                Identifier = "user",
                Secret = "wrong",
                Device = TestDevice.Default(),
            });

        // try again with correct password
        var result = await orchestrator.LoginAsync(flow,
            new LoginRequest
            {
                Tenant = TenantKey.Single,
                Identifier = "user",
                Secret = "user",
                Device = TestDevice.Default(),
            });

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Locked_user_should_not_increment_failed_attempts()
    {
        var runtime = new TestAuthRuntime<UserKey>(configureServer: o =>
        {
            o.Login.MaxFailedAttempts = 1;
        });

        var orchestrator = runtime.GetLoginOrchestrator();
        var flow = await runtime.CreateLoginFlowAsync();

        await orchestrator.LoginAsync(flow,
            new LoginRequest
            {
                Tenant = TenantKey.Single,
                Identifier = "user",
                Secret = "wrong",
                Device = TestDevice.Default(),
            });

        var store = runtime.Services.GetRequiredService<InMemoryUserSecurityStore<UserKey>>();
        var state1 = store.GetState(TenantKey.Single, UserKey.Parse("user", null));

        await orchestrator.LoginAsync(flow,
            new LoginRequest
            {
                Tenant = TenantKey.Single,
                Identifier = "user",
                Secret = "wrong",
                Device = TestDevice.Default(),
            });

        var state2 = store.GetState(TenantKey.Single, UserKey.Parse("user", null));

        state2!.FailedLoginAttempts.Should().Be(state1!.FailedLoginAttempts);
    }

    [Fact]
    public async Task MaxFailedAttempts_zero_should_disable_lockout()
    {
        var runtime = new TestAuthRuntime<UserKey>(configureServer: o =>
        {
            o.Login.MaxFailedAttempts = 0;
        });

        var orchestrator = runtime.GetLoginOrchestrator();
        var flow = await runtime.CreateLoginFlowAsync();

        for (int i = 0; i < 5; i++)
        {
            await orchestrator.LoginAsync(flow,
                new LoginRequest
                {
                    Tenant = TenantKey.Single,
                    Identifier = "user",
                    Secret = "wrong",
                    Device = TestDevice.Default(),
                });
        }

        var store = runtime.Services.GetRequiredService<InMemoryUserSecurityStore<UserKey>>();
        var state = store.GetState(TenantKey.Single, UserKey.Parse("user", null));

        state!.IsLocked.Should().BeFalse();
        state.FailedLoginAttempts.Should().Be(5);
    }

    [Fact]
    public async Task Invalid_device_id_should_throw_security_exception()
    {
        var runtime = new TestAuthRuntime<UserKey>();
        var orchestrator = runtime.GetLoginOrchestrator();
        var flow = await runtime.CreateLoginFlowAsync();

        Func<Task> act = () => orchestrator.LoginAsync(flow,
            new LoginRequest
            {
                Tenant = TenantKey.Single,
                Identifier = "user",
                Secret = "user",
                Device = DeviceContext.FromDeviceId(DeviceId.Create("x")), // too short
            });

        await act.Should().ThrowAsync<SecurityException>();
    }

    [Fact]
    public async Task Locked_user_failed_login_should_not_extend_lockout_duration()
    {
        var runtime = new TestAuthRuntime<UserKey>(configureServer: o =>
        {
            o.Login.MaxFailedAttempts = 1;
            o.Login.LockoutMinutes = 15;
        });

        var orchestrator = runtime.GetLoginOrchestrator();
        var flow = await runtime.CreateLoginFlowAsync();

        await orchestrator.LoginAsync(flow,
            new LoginRequest
            {
                Tenant = TenantKey.Single,
                Identifier = "user",
                Secret = "wrong",
                Device = TestDevice.Default(),
            });

        var store = runtime.Services.GetRequiredService<InMemoryUserSecurityStore<UserKey>>();
        var state1 = store.GetState(TenantKey.Single, UserKey.Parse("user", null));

        var lockedUntil = state1!.LockedUntil;

        await orchestrator.LoginAsync(flow,
            new LoginRequest
            {
                Tenant = TenantKey.Single,
                Identifier = "user",
                Secret = "wrong",
                Device = TestDevice.Default(),
            });

        var state2 = store.GetState(TenantKey.Single, UserKey.Parse("user", null));
        state2!.LockedUntil.Should().Be(lockedUntil);
    }
}
