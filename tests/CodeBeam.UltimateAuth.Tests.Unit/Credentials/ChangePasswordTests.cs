using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class ChangePasswordTests
{
    [Fact]
    public async Task Change_password_with_correct_current_should_succeed()
    {
        var runtime = new TestAuthRuntime<UserKey>();
        var service = runtime.GetCredentialManagementService();

        var context = TestAccessContext.ForUser(TestUsers.User, UAuthActions.Credentials.ChangeSelf);

        var result = await service.ChangeSecretAsync(context,
            new ChangeCredentialRequest
            {
                CurrentSecret = "user",
                NewSecret = "newpass123"
            });

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Change_password_with_wrong_current_should_throw()
    {
        var runtime = new TestAuthRuntime<UserKey>();
        var service = runtime.GetCredentialManagementService();

        var context = TestAccessContext.ForUser(TestUsers.User, UAuthActions.Credentials.ChangeSelf);

        Func<Task> act = async () =>
            await service.ChangeSecretAsync(context,
                new ChangeCredentialRequest
                {
                    CurrentSecret = "wrong",
                    NewSecret = "newpass123"
                });

        await act.Should().ThrowAsync<UAuthConflictException>();
    }

    [Fact]
    public async Task Change_password_to_same_should_throw()
    {
        var runtime = new TestAuthRuntime<UserKey>();
        var service = runtime.GetCredentialManagementService();

        var context = TestAccessContext.ForUser(TestUsers.User, UAuthActions.Credentials.ChangeSelf);

        Func<Task> act = async () =>
            await service.ChangeSecretAsync(context,
                new ChangeCredentialRequest
                {
                    CurrentSecret = "user",
                    NewSecret = "user"
                });

        await act.Should().ThrowAsync<UAuthValidationException>();
    }

    [Fact]
    public async Task Change_password_should_increment_version()
    {
        var runtime = new TestAuthRuntime<UserKey>();
        var service = runtime.GetCredentialManagementService();

        var context = TestAccessContext.ForUser(TestUsers.User, UAuthActions.Credentials.ChangeSelf);

        var before = await service.GetAllAsync(context);
        var versionBefore = before.Credentials.Single().Version;

        await service.ChangeSecretAsync(context,
            new ChangeCredentialRequest
            {
                CurrentSecret = "user",
                NewSecret = "newpass123"
            });

        var after = await service.GetAllAsync(context);
        var versionAfter = after.Credentials.Single().Version;

        versionAfter.Should().Be(versionBefore + 1);
    }

    [Fact]
    public async Task Old_password_should_not_work_after_change()
    {
        var runtime = new TestAuthRuntime<UserKey>();
        var service = runtime.GetCredentialManagementService();
        var orchestrator = runtime.GetLoginOrchestrator();
        var flow = await runtime.CreateLoginFlowAsync();

        var context = TestAccessContext.ForUser(TestUsers.User, UAuthActions.Credentials.ChangeSelf);

        await service.ChangeSecretAsync(context,
            new ChangeCredentialRequest
            {
                CurrentSecret = "user",
                NewSecret = "newpass123"
            });

        var oldLogin = await orchestrator.LoginAsync(flow,
            new LoginRequest
            {
                Tenant = TenantKey.Single,
                Identifier = "user",
                Secret = "user"
            });

        oldLogin.IsSuccess.Should().BeFalse();

        var newLogin = await orchestrator.LoginAsync(flow,
            new LoginRequest
            {
                Tenant = TenantKey.Single,
                Identifier = "user",
                Secret = "newpass123"
            });

        newLogin.IsSuccess.Should().BeTrue();
    }


}
