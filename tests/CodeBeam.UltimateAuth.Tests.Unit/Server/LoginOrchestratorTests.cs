using CodeBeam.UltimateAuth.Authentication.InMemory;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Events;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

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
            });

        var factory = runtime.Services.GetRequiredService<IAuthenticationSecurityStateStoreFactory>();
        var store = factory.Create(TenantKeys.Single);
        var state = await store.GetAsync(TestUsers.User, AuthenticationSecurityScope.Factor, CredentialType.Password);
        state?.FailedAttempts.Should().Be(1);
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
            });

        await orchestrator.LoginAsync(flow,
            new LoginRequest
            {
                Tenant = TenantKey.Single,
                Identifier = "user",
                Secret = "user", // valid password
            });

        var factory = runtime.Services.GetRequiredService<IAuthenticationSecurityStateStoreFactory>();
        var store = factory.Create(TenantKeys.Single);
        var state = await store.GetAsync(TestUsers.User, AuthenticationSecurityScope.Factor, CredentialType.Password);
        state?.FailedAttempts.Should().Be(0);
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
            });

        var factory = runtime.Services.GetRequiredService<IAuthenticationSecurityStateStoreFactory>();
        var store = factory.Create(TenantKeys.Single);
        var state = await store.GetAsync(TestUsers.User, AuthenticationSecurityScope.Factor, CredentialType.Password);

        state!.IsLocked(DateTimeOffset.UtcNow).Should().BeTrue();
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

        await orchestrator.LoginAsync(flow,
            new LoginRequest
            {
                Tenant = TenantKey.Single,
                Identifier = "user",
                Secret = "wrong",
            });

        var result = await orchestrator.LoginAsync(flow,
            new LoginRequest
            {
                Tenant = TenantKey.Single,
                Identifier = "user",
                Secret = "user",
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
            });

        var factory = runtime.Services.GetRequiredService<IAuthenticationSecurityStateStoreFactory>();
        var store = factory.Create(TenantKeys.Single);
        var state1 = await store.GetAsync(TestUsers.User, AuthenticationSecurityScope.Factor, CredentialType.Password);

        await orchestrator.LoginAsync(flow,
            new LoginRequest
            {
                Tenant = TenantKey.Single,
                Identifier = "user",
            });

        var state2 = await store.GetAsync(TestUsers.User, AuthenticationSecurityScope.Factor, CredentialType.Password);
        state2?.FailedAttempts.Should().Be(state1!.FailedAttempts);
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
                });
        }

        var factory = runtime.Services.GetRequiredService<IAuthenticationSecurityStateStoreFactory>();
        var store = factory.Create(TenantKeys.Single);
        var state = await store.GetAsync(TestUsers.User, AuthenticationSecurityScope.Factor, CredentialType.Password);

        state?.IsLocked(DateTimeOffset.UtcNow).Should().BeFalse();
        state?.FailedAttempts.Should().Be(5);
    }

    [Fact]
    public async Task Locked_user_failed_login_should_not_extend_lockout_duration()
    {
        var runtime = new TestAuthRuntime<UserKey>(configureServer: o =>
        {
            o.Login.MaxFailedAttempts = 1;
            o.Login.LockoutDuration = TimeSpan.FromMinutes(15);
        });

        var orchestrator = runtime.GetLoginOrchestrator();
        var flow = await runtime.CreateLoginFlowAsync();

        await orchestrator.LoginAsync(flow,
            new LoginRequest
            {
                Tenant = TenantKey.Single,
                Identifier = "user",
                Secret = "wrong",
            });

        var factory = runtime.Services.GetRequiredService<IAuthenticationSecurityStateStoreFactory>();
        var store = factory.Create(TenantKeys.Single);
        var state1 = await store.GetAsync(TestUsers.User, AuthenticationSecurityScope.Factor, CredentialType.Password);

        var lockedUntil = state1!.LockedUntil;

        await orchestrator.LoginAsync(flow,
            new LoginRequest
            {
                Tenant = TenantKey.Single,
                Identifier = "user",
                Secret = "wrong",
            });

        var state2 = await store.GetAsync(TestUsers.User, AuthenticationSecurityScope.Factor, CredentialType.Password);
        state2?.LockedUntil.Should().Be(lockedUntil);
    }

    [Fact]
    public async Task Login_success_should_trigger_UserLoggedIn_event()
    {
        UserLoggedInContext? captured = null;

        var runtime = new TestAuthRuntime<UserKey>(configureServer: o =>
        {
            o.Events.OnUserLoggedIn = ctx =>
            {
                captured = ctx;
                return Task.CompletedTask;
            };
        });

        var orchestrator = runtime.GetLoginOrchestrator();
        var flow = await runtime.CreateLoginFlowAsync();

        await orchestrator.LoginAsync(flow, new LoginRequest
        {
            Tenant = TenantKey.Single,
            Identifier = "user",
            Secret = "user",
        });

        captured.Should().NotBeNull();
        captured!.UserKey.Should().Be(TestUsers.User);
    }

    [Fact]
    public async Task Login_success_should_trigger_OnAnyEvent()
    {
        var count = 0;

        var runtime = new TestAuthRuntime<UserKey>(configureServer: o =>
        {
            o.Events.OnAnyEvent = _ =>
            {
                count++;
                return Task.CompletedTask;
            };
        });

        var orchestrator = runtime.GetLoginOrchestrator();
        var flow = await runtime.CreateLoginFlowAsync();

        await orchestrator.LoginAsync(flow, new LoginRequest
        {
            Tenant = TenantKey.Single,
            Identifier = "user",
            Secret = "user",
        });

        count.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Event_handler_exception_should_not_break_login_flow()
    {
        var runtime = new TestAuthRuntime<UserKey>(configureServer: o =>
        {
            o.Events.OnUserLoggedIn = _ => throw new Exception("boom");
        });

        var orchestrator = runtime.GetLoginOrchestrator();
        var flow = await runtime.CreateLoginFlowAsync();

        var result = await orchestrator.LoginAsync(flow, new LoginRequest
        {
            Tenant = TenantKey.Single,
            Identifier = "user",
            Secret = "user",
        });

        result.IsSuccess.Should().BeTrue();
    }
}
