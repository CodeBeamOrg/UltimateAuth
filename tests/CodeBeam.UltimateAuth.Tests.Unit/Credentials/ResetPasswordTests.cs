using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class ResetPasswordTests
{
    [Fact]
    public async Task Begin_reset_with_token_should_return_token()
    {
        var runtime = new TestAuthRuntime<UserKey>();
        var service = runtime.GetCredentialManagementService();

        var context = TestAccessContext.WithAction(UAuthActions.Credentials.BeginResetAnonymous);

        var result = await service.BeginResetAsync(context,
            new BeginCredentialResetRequest
            {
                Identifier = "admin",
                CredentialType = CredentialType.Password,
                ResetCodeType = ResetCodeType.Token
            });

        result.Token.Should().NotBeNull();
        result.Token!.Length.Should().BeGreaterThan(20);
        result.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public async Task Begin_reset_with_code_should_return_numeric_code()
    {
        var runtime = new TestAuthRuntime<UserKey>();
        var service = runtime.GetCredentialManagementService();

        var context = TestAccessContext.WithAction(UAuthActions.Credentials.BeginResetAnonymous);

        var result = await service.BeginResetAsync(context,
            new BeginCredentialResetRequest
            {
                Identifier = "admin",
                CredentialType = CredentialType.Password,
                ResetCodeType = ResetCodeType.Code
            });

        result.Token.Should().NotBeNull();
        result.Token!.Should().MatchRegex("^[0-9]{6}$");
    }

    [Fact]
    public async Task Begin_reset_for_unknown_user_should_not_fail()
    {
        var runtime = new TestAuthRuntime<UserKey>();
        var service = runtime.GetCredentialManagementService();

        var context = TestAccessContext.WithAction(UAuthActions.Credentials.BeginResetAnonymous);

        var result = await service.BeginResetAsync(context,
            new BeginCredentialResetRequest
            {
                Identifier = "unknown@test.com",
                CredentialType = CredentialType.Password,
                ResetCodeType = ResetCodeType.Token
            });

        result.Token.Should().BeNull();
    }

    [Fact]
    public async Task Reset_password_with_valid_token_should_succeed()
    {
        var runtime = new TestAuthRuntime<UserKey>();
        var service = runtime.GetCredentialManagementService();

        var begin = await service.BeginResetAsync(
            TestAccessContext.WithAction(UAuthActions.Credentials.BeginResetAnonymous),
            new BeginCredentialResetRequest
            {
                Identifier = "admin",
                CredentialType = CredentialType.Password,
                ResetCodeType = ResetCodeType.Token
            });

        var result = await service.CompleteResetAsync(
            TestAccessContext.WithAction(UAuthActions.Credentials.CompleteResetAnonymous),
            new CompleteCredentialResetRequest
            {
                Identifier = "admin",
                CredentialType = CredentialType.Password,
                ResetToken = begin.Token!,
                NewSecret = "newpass123"
            });

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Reset_password_with_same_password_should_fail()
    {
        var runtime = new TestAuthRuntime<UserKey>();
        var service = runtime.GetCredentialManagementService();

        var begin = await service.BeginResetAsync(
            TestAccessContext.WithAction(UAuthActions.Credentials.BeginResetAnonymous),
            new BeginCredentialResetRequest
            {
                Identifier = "admin",
                CredentialType = CredentialType.Password,
                ResetCodeType = ResetCodeType.Token
            });

        Func<Task> act = async () =>
            await service.CompleteResetAsync(
                TestAccessContext.WithAction(UAuthActions.Credentials.CompleteResetAnonymous),
                new CompleteCredentialResetRequest
                {
                    Identifier = "admin",
                    CredentialType = CredentialType.Password,
                    ResetToken = begin.Token!,
                    NewSecret = "admin"
                });

        await act.Should().ThrowAsync<UAuthValidationException>();
    }

    [Fact]
    public async Task Reset_token_should_lock_after_max_attempts()
    {
        var runtime = new TestAuthRuntime<UserKey>();
        var service = runtime.GetCredentialManagementService();

        var begin = await service.BeginResetAsync(
            TestAccessContext.WithAction(UAuthActions.Credentials.BeginResetAnonymous),
            new BeginCredentialResetRequest
            {
                Identifier = "admin",
                CredentialType = CredentialType.Password,
                ResetCodeType = ResetCodeType.Code
            });

        for (int i = 0; i < 3; i++)
        {
            try
            {
                await service.CompleteResetAsync(
                    TestAccessContext.WithAction(UAuthActions.Credentials.CompleteResetAnonymous),
                    new CompleteCredentialResetRequest
                    {
                        Identifier = "admin",
                        CredentialType = CredentialType.Password,
                        ResetToken = begin!.Token == "000000" ? "000001" : "000000",
                        NewSecret = "newpass123"
                    });
            }
            catch { }
        }

        Func<Task> act = async () =>
            await service.CompleteResetAsync(
                TestAccessContext.WithAction(UAuthActions.Credentials.CompleteResetAnonymous),
                new CompleteCredentialResetRequest
                {
                    Identifier = "admin",
                    CredentialType = CredentialType.Password,
                    ResetToken = begin.Token!,
                    NewSecret = "newpass123"
                });

        await act.Should().ThrowAsync<UAuthConflictException>();
    }

    [Fact]
    public async Task Reset_token_should_be_single_use()
    {
        var runtime = new TestAuthRuntime<UserKey>();
        var service = runtime.GetCredentialManagementService();

        var begin = await service.BeginResetAsync(
            TestAccessContext.WithAction(UAuthActions.Credentials.BeginResetAnonymous),
            new BeginCredentialResetRequest
            {
                Identifier = "admin",
                CredentialType = CredentialType.Password,
                ResetCodeType = ResetCodeType.Token
            });

        await service.CompleteResetAsync(
            TestAccessContext.WithAction(UAuthActions.Credentials.CompleteResetAnonymous),
            new CompleteCredentialResetRequest
            {
                Identifier = "admin",
                CredentialType = CredentialType.Password,
                ResetToken = begin.Token!,
                NewSecret = "newpass123"
            });

        Func<Task> act = async () =>
            await service.CompleteResetAsync(
                TestAccessContext.WithAction(UAuthActions.Credentials.CompleteResetAnonymous),
                new CompleteCredentialResetRequest
                {
                    Identifier = "admin",
                    CredentialType = CredentialType.Password,
                    ResetToken = begin.Token!,
                    NewSecret = "anotherpass"
                });

        await act.Should().ThrowAsync<UAuthConflictException>();
    }

    [Fact]
    public async Task Reset_token_should_fail_if_expired()
    {
        var runtime = new TestAuthRuntime<UserKey>();
        var service = runtime.GetCredentialManagementService();
        var clock = runtime.Clock;

        var begin = await service.BeginResetAsync(
            TestAccessContext.WithAction(UAuthActions.Credentials.BeginResetAnonymous),
            new BeginCredentialResetRequest
            {
                Identifier = "admin",
                CredentialType = CredentialType.Password,
                ResetCodeType = ResetCodeType.Token
            });

        clock.Advance(TimeSpan.FromMinutes(45));

        Func<Task> act = async () =>
            await service.CompleteResetAsync(
                TestAccessContext.WithAction(UAuthActions.Credentials.CompleteResetAnonymous),
                new CompleteCredentialResetRequest
                {
                    Identifier = "admin",
                    CredentialType = CredentialType.Password,
                    ResetToken = begin.Token!,
                    NewSecret = "newpass123"
                });

        await act.Should().ThrowAsync<UAuthConflictException>();
    }
}
