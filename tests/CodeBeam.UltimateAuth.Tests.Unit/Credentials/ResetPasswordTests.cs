using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Credentials;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class ResetPasswordTests
{
    [Fact]
    public async Task Begin_reset_with_token_should_notify_without_exposing_token()
    {
        var (runtime, notifier) = CreateRuntime();
        var service = runtime.GetCredentialManagementService();

        var result = await service.BeginResetAsync(
            BeginContext(),
            BeginRequest(ResetCodeType.Token));

        var notification = notifier.Notifications.Should()
            .ContainSingle().Subject;

        notification.Token.Should().NotBeNullOrWhiteSpace();
        notification.Token.Length.Should().BeGreaterThan(20);
        notification.CodeType.Should().Be(ResetCodeType.Token);
        notification.CredentialType.Should().Be(CredentialType.Password);
        notification.ExpiresAt.Should().Be(result.ExpiresAt);
        result.ExpiresAt.Should().BeAfter(runtime.Clock.UtcNow);

        // The public JSON response must not contain the reset proof.
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(result, new JsonSerializerOptions(JsonSerializerDefaults.Web)));

        json.RootElement.TryGetProperty("token", out _).Should().BeFalse();
        JsonSerializer.Serialize(result)
            .Should().NotContain(notification.Token);

        var json1 = JsonSerializer.Serialize(result, new JsonSerializerOptions(JsonSerializerDefaults.Web));

        using var document = JsonDocument.Parse(json1);

        document.RootElement
            .EnumerateObject()
            .Select(property => property.Name)
            .Should()
            .BeEquivalentTo(
                new[] { "expiresAt" },
                "public reset response must contain only the approved fields");

        json1.Should().NotContain(
            notification.Token,
            "the reset proof must be delivered only through the notifier");
    }

    [Fact]
    public async Task Begin_reset_with_code_should_notify_numeric_code()
    {
        var (runtime, notifier) = CreateRuntime();

        await runtime.GetCredentialManagementService().BeginResetAsync(
            BeginContext(),
            BeginRequest(ResetCodeType.Code));

        var notification = notifier.Notifications.Should()
            .ContainSingle().Subject;

        notification.CodeType.Should().Be(ResetCodeType.Code);
        notification.Token.Should().MatchRegex("^[0-9]{6}$");
    }

    [Fact]
    public async Task Begin_reset_for_unknown_user_should_not_notify()
    {
        var (runtime, notifier) = CreateRuntime();

        var result = await runtime.GetCredentialManagementService()
            .BeginResetAsync(
                BeginContext(),
                BeginRequest(
                    ResetCodeType.Token,
                    "unknown@test.com"));

        result.Should().NotBeNull();
        result.ExpiresAt.Should().BeAfter(runtime.Clock.UtcNow);
        notifier.Notifications.Should().BeEmpty();
    }

    [Fact]
    public async Task Reset_password_with_valid_token_should_succeed()
    {
        var (runtime, notifier) = CreateRuntime();
        var service = runtime.GetCredentialManagementService();

        var token = await BeginAndCaptureAsync(
            runtime, notifier, ResetCodeType.Token);

        var result = await service.CompleteResetAsync(
            CompleteContext(),
            CompleteRequest(token, "newpass123"));

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task Reset_password_with_same_password_should_fail()
    {
        var (runtime, notifier) = CreateRuntime();
        var service = runtime.GetCredentialManagementService();

        var token = await BeginAndCaptureAsync(
            runtime, notifier, ResetCodeType.Token);

        Func<Task> act = async () =>
            await service.CompleteResetAsync(
                CompleteContext(),
                CompleteRequest(token, "admin"));

        await act.Should().ThrowAsync<UAuthValidationException>();
    }

    [Fact]
    public async Task Reset_token_should_lock_after_max_attempts()
    {
        const int maxAttempts = 3;
        var (runtime, notifier) = CreateRuntime(maxAttempts);
        var service = runtime.GetCredentialManagementService();

        var token = await BeginAndCaptureAsync(
            runtime, notifier, ResetCodeType.Code);

        var wrongToken = token == "000000" ? "000001" : "000000";

        for (var i = 0; i < maxAttempts; i++)
        {
            Func<Task> invalidAttempt = async () =>
                await service.CompleteResetAsync(
                    CompleteContext(),
                    CompleteRequest(wrongToken, "newpass123"));

            // Do not swallow unrelated failures.
            await invalidAttempt.Should()
                .ThrowAsync<UAuthConflictException>();
        }

        Func<Task> act = async () =>
            await service.CompleteResetAsync(
                CompleteContext(),
                CompleteRequest(token, "newpass123"));

        await act.Should().ThrowAsync<UAuthConflictException>();
    }

    [Fact]
    public async Task Reset_token_should_be_single_use()
    {
        var (runtime, notifier) = CreateRuntime();
        var service = runtime.GetCredentialManagementService();

        var token = await BeginAndCaptureAsync(
            runtime, notifier, ResetCodeType.Token);

        var first = await service.CompleteResetAsync(
            CompleteContext(),
            CompleteRequest(token, "newpass123"));

        first.Succeeded.Should().BeTrue();

        Func<Task> act = async () =>
            await service.CompleteResetAsync(
                CompleteContext(),
                CompleteRequest(token, "anotherpass"));

        await act.Should().ThrowAsync<UAuthConflictException>();
    }

    [Fact]
    public async Task Reset_token_should_fail_if_expired()
    {
        var (runtime, notifier) = CreateRuntime();
        var service = runtime.GetCredentialManagementService();

        var token = await BeginAndCaptureAsync(
            runtime, notifier, ResetCodeType.Token);

        var notification = notifier.Notifications.Single();

        // Advance past the actual configured expiry.
        runtime.Clock.Advance(
            notification.ExpiresAt - runtime.Clock.UtcNow
            + TimeSpan.FromSeconds(1));

        Func<Task> act = async () =>
            await service.CompleteResetAsync(
                CompleteContext(),
                CompleteRequest(token, "newpass123"));

        await act.Should().ThrowAsync<UAuthConflictException>();
    }

    private static (
        TestAuthRuntime<UserKey> Runtime,
        RecordingCredentialResetNotifier Notifier)
        CreateRuntime(int maxAttempts = 3)
    {
        var notifier = new RecordingCredentialResetNotifier();

        var runtime = new TestAuthRuntime<UserKey>(
            configureServer: options =>
                options.ResetCredential.MaxAttempts = maxAttempts,
            configureServices: services =>
                services.AddSingleton<ICredentialResetNotifier>(notifier));

        return (runtime, notifier);
    }

    private static async Task<string> BeginAndCaptureAsync(
        TestAuthRuntime<UserKey> runtime,
        RecordingCredentialResetNotifier notifier,
        ResetCodeType codeType)
    {
        await runtime.GetCredentialManagementService().BeginResetAsync(
            BeginContext(),
            BeginRequest(codeType));

        return notifier.Notifications.Should()
            .ContainSingle().Subject.Token;
    }

    private static CodeBeam.UltimateAuth.Core.Contracts.AccessContext
        BeginContext() =>
        TestAccessContext.WithAction(
            UAuthActions.Credentials.BeginResetAnonymous);

    private static CodeBeam.UltimateAuth.Core.Contracts.AccessContext
        CompleteContext() =>
        TestAccessContext.WithAction(
            UAuthActions.Credentials.CompleteResetAnonymous);

    private static BeginResetCredentialRequest BeginRequest(
        ResetCodeType codeType,
        string identifier = "admin") => new()
        {
            Identifier = identifier,
            CredentialType = CredentialType.Password,
            ResetCodeType = codeType
        };

    private static CompleteResetCredentialRequest CompleteRequest(
        string token,
        string newSecret) => new()
        {
            Identifier = "admin",
            CredentialType = CredentialType.Password,
            ResetToken = token,
            NewSecret = newSecret
        };

    private sealed class RecordingCredentialResetNotifier
        : ICredentialResetNotifier
    {
        public List<CredentialResetNotification> Notifications { get; } = [];

        public Task NotifyAsync(
            CredentialResetNotification notification,
            CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();
            Notifications.Add(notification);
            return Task.CompletedTask;
        }
    }
}
