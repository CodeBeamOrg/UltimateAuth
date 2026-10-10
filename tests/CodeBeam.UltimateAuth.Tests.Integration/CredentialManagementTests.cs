using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Credentials;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Reference;
using CodeBeam.UltimateAuth.Tests.Integration.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;

namespace CodeBeam.UltimateAuth.Tests.Integration;

public sealed class CredentialManagementTests
{
    [Fact]
    public async Task AnonymousReset_WithValidToken_ChangesPassword()
    {
        var notifier = new TestResetNotifier();

        using var factory = AuthServerFactory.CreateWithServices(services =>
        {
            services.RemoveAll<ICredentialResetNotifier>();
            services.AddSingleton<ICredentialResetNotifier>(notifier);
        });

        factory.Clock.Reset();

        var user = await factory.CreateLoginUserAsync();

        using var client = CreateClient(factory);

        var beginResponse = await client.PostAsJsonAsync(
            "/auth/me/credentials/reset/begin",
            new BeginResetCredentialRequest
            {
                Identifier = user.Identifier,
                CredentialType = CredentialType.Password,
                ResetCodeType = ResetCodeType.Token
            });

        beginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var notification = notifier.Notifications
            .Should()
            .ContainSingle(x => x.UserKey == user.UserKey)
            .Subject;

        var newPassword = $"Changed-{Guid.NewGuid():N}!";

        var completeResponse = await client.PostAsJsonAsync(
            "/auth/me/credentials/reset/complete",
            new CompleteResetCredentialRequest
            {
                Identifier = user.Identifier,
                CredentialType = CredentialType.Password,
                ResetToken = notification.Token,
                NewSecret = newPassword
            });

        completeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        // Credential'ın gerçekten değiştiğini doğrula.
        using var scope = factory.Services.CreateScope();

        var storeFactory = scope.ServiceProvider
            .GetRequiredService<IPasswordCredentialStoreFactory>();

        var hasher = scope.ServiceProvider
            .GetRequiredService<IUAuthPasswordHasher>();

        var store = storeFactory.Create(notification.Tenant);

        var credentials = await store.GetByUserAsync(user.UserKey);

        var password = credentials
            .OfType<PasswordCredential>()
            .Single();

        hasher.Verify(password.SecretHash, newPassword)
            .Should().BeTrue();

        hasher.Verify(password.SecretHash, user.Secret)
            .Should().BeFalse();
    }


    [Fact]
    public async Task AnonymousBeginReset_UnknownIdentifier_ShouldNotRevealUser()
    {
        var notifier = new TestResetNotifier();

        using var factory = CreateFactory(notifier);
        factory.Clock.Reset();

        using var client = CreateClient(factory);

        var response = await BeginResetAsync(
            client,
            "/auth/me/credentials/reset/begin",
            $"unknown-{Guid.NewGuid():N}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        notifier.Notifications.Should().BeEmpty();
    }

    [Fact]
    public async Task AdminBeginReset_TargetMismatch_ShouldRejectWithoutNotification()
    {
        var notifier = new TestResetNotifier();

        using var factory = CreateFactory(notifier);
        factory.Clock.Reset();

        var admin = await factory.CreateLoginUserAsync();
        var target = await factory.CreateLoginUserAsync();
        var other = await factory.CreateLoginUserAsync();

        await factory.GrantPermissionsAsync(
            admin.UserKey,
            [UAuthActions.Credentials.BeginResetAdmin]);

        using var client = CreateClient(factory);
        await AuthenticateAsync(client, admin);

        var response = await BeginResetAsync(
            client,
            $"/auth/admin/users/{target.UserKey.Value}/credentials/reset/begin",
            other.Identifier);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        notifier.Notifications.Should().BeEmpty();

        await AssertPasswordAsync(factory, other, other.Secret);
    }

    [Fact]
    public async Task AdminBeginReset_ValidTarget_ShouldNotifyTarget()
    {
        var notifier = new TestResetNotifier();

        using var factory = CreateFactory(notifier);
        factory.Clock.Reset();

        var admin = await factory.CreateLoginUserAsync();
        var target = await factory.CreateLoginUserAsync();

        await factory.GrantPermissionsAsync(
            admin.UserKey,
            [UAuthActions.Credentials.BeginResetAdmin]);

        using var client = CreateClient(factory);
        await AuthenticateAsync(client, admin);

        var response = await BeginResetAsync(
            client,
            $"/auth/admin/users/{target.UserKey.Value}/credentials/reset/begin",
            target.Identifier);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        notifier.Notifications.Should()
            .ContainSingle(x => x.UserKey == target.UserKey);
    }

    [Fact]
    public async Task AdminCompleteReset_TargetMismatch_ShouldNotChangeOtherPassword()
    {
        var notifier = new TestResetNotifier();

        using var factory = CreateFactory(notifier);
        factory.Clock.Reset();

        var admin = await factory.CreateLoginUserAsync();
        var target = await factory.CreateLoginUserAsync();
        var other = await factory.CreateLoginUserAsync();

        await factory.GrantPermissionsAsync(
            admin.UserKey,
            [UAuthActions.Credentials.CompleteResetAdmin]);

        // B için gerçekten geçerli bir reset token'ı üret.
        using var anonymousClient = CreateClient(factory);

        var begin = await BeginResetAsync(anonymousClient, "/auth/me/credentials/reset/begin", other.Identifier);

        begin.StatusCode.Should().Be(HttpStatusCode.OK);

        var token = notifier.Notifications
            .Single(x => x.UserKey == other.UserKey)
            .Token;

        using var adminClient = CreateClient(factory);
        await AuthenticateAsync(adminClient, admin);

        var newPassword = $"Changed-{Guid.NewGuid():N}!";

        var response = await CompleteResetAsync(
            adminClient,
            $"/auth/admin/users/{target.UserKey.Value}/credentials/reset/complete",
            other.Identifier,
            token,
            newPassword);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        await AssertPasswordAsync(factory, other, other.Secret);
        await AssertPasswordRejectedAsync(factory, other, newPassword);
    }

    [Fact]
    public async Task AdminCompleteReset_ValidTarget_ShouldChangePassword()
    {
        var notifier = new TestResetNotifier();

        using var factory = CreateFactory(notifier);
        factory.Clock.Reset();

        var admin = await factory.CreateLoginUserAsync();
        var target = await factory.CreateLoginUserAsync();

        await factory.GrantPermissionsAsync(
            admin.UserKey,
            [
                UAuthActions.Credentials.BeginResetAdmin,
            UAuthActions.Credentials.CompleteResetAdmin
            ]);

        using var client = CreateClient(factory);
        await AuthenticateAsync(client, admin);

        var url =
            $"/auth/admin/users/{target.UserKey.Value}/credentials/reset";

        var begin = await BeginResetAsync(
            client,
            $"{url}/begin",
            target.Identifier);

        begin.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var token = notifier.Notifications
            .Single(x => x.UserKey == target.UserKey)
            .Token;

        var newPassword = $"Changed-{Guid.NewGuid():N}!";

        var complete = await CompleteResetAsync(
            client,
            $"{url}/complete",
            target.Identifier,
            token,
            newPassword);

        complete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        await AssertPasswordAsync(factory, target, newPassword);
        await AssertPasswordRejectedAsync(factory, target, target.Secret);
    }

    [Fact]
    public async Task AnonymousCompleteReset_ReusedToken_ShouldReject()
    {
        var notifier = new TestResetNotifier();

        using var factory = CreateFactory(notifier);
        factory.Clock.Reset();

        var user = await factory.CreateLoginUserAsync();

        using var client = CreateClient(factory);

        var begin = await BeginResetAsync(
            client,
            "/auth/me/credentials/reset/begin",
            user.Identifier);

        begin.StatusCode.Should().Be(HttpStatusCode.OK);

        var token = notifier.Notifications.Single().Token;
        var newPassword = $"Changed-{Guid.NewGuid():N}!";
        var replayPassword = $"Replay-{Guid.NewGuid():N}!";

        var first = await CompleteResetAsync(
            client,
            "/auth/me/credentials/reset/complete",
            user.Identifier,
            token,
            newPassword);

        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var replay = await CompleteResetAsync(
            client,
            "/auth/me/credentials/reset/complete",
            user.Identifier,
            token,
            replayPassword);

        // Should be true for enumeration.
        replay.IsSuccessStatusCode.Should().BeTrue();

        await AssertPasswordAsync(factory, user, newPassword);
        await AssertPasswordRejectedAsync(factory, user, replayPassword);
    }

    [Fact]
    public async Task AdminBeginReset_WithoutPermission_ShouldNotNotify()
    {
        var notifier = new TestResetNotifier();

        using var factory = CreateFactory(notifier);
        factory.Clock.Reset();

        var actor = await factory.CreateLoginUserAsync();
        var target = await factory.CreateLoginUserAsync();

        using var client = CreateClient(factory);
        await AuthenticateAsync(client, actor);

        var response = await BeginResetAsync(
            client,
            $"/auth/admin/users/{target.UserKey.Value}/credentials/reset/begin",
            target.Identifier);

        response.IsSuccessStatusCode.Should().BeFalse();
        notifier.Notifications.Should().BeEmpty();
    }

    [Fact]
    public async Task AdminBeginReset_Unauthenticated_ShouldReject()
    {
        var notifier = new TestResetNotifier();

        using var factory = CreateFactory(notifier);
        factory.Clock.Reset();

        var target = await factory.CreateLoginUserAsync();

        using var client = CreateClient(factory);

        var response = await BeginResetAsync(
            client,
            $"/auth/admin/users/{target.UserKey.Value}/credentials/reset/begin",
            target.Identifier);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        notifier.Notifications.Should().BeEmpty();
    }


    [Fact]
    public async Task AnonymousCompleteReset_InvalidProof_ShouldNotRevealIdentifierExistence()
    {
        var notifier = new TestResetNotifier();

        using var factory = CreateFactory(notifier);
        factory.Clock.Reset();

        var existingUser = await factory.CreateLoginUserAsync();

        using var client = CreateClient(factory);

        var unknownIdentifier = $"unknown-{Guid.NewGuid():N}";
        const string invalidToken = "invalid-reset-proof";

        var existingResponse = await CompleteResetAsync(
            client,
            "/auth/me/credentials/reset/complete",
            existingUser.Identifier,
            invalidToken,
            "New-Password-123!");

        var unknownResponse = await CompleteResetAsync(
            client,
            "/auth/me/credentials/reset/complete",
            unknownIdentifier,
            invalidToken,
            "New-Password-123!");

        // Same observable HTTP status.
        existingResponse.StatusCode.Should()
            .Be(unknownResponse.StatusCode);

        // Same observable response body.
        var existingBody = await existingResponse.Content.ReadAsStringAsync();
        var unknownBody = await unknownResponse.Content.ReadAsStringAsync();

        existingBody.Should().Be(unknownBody);

        // Existing user's credential must remain unchanged.
        await AssertPasswordAsync(
            factory,
            existingUser,
            existingUser.Secret);
    }



    private static HttpClient CreateClient(AuthServerFactory factory)
    {
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = false
            });

        client.DefaultRequestHeaders.Add(
            "Origin",
            "https://localhost:6130");

        client.DefaultRequestHeaders.Add(
            "X-UDID",
            $"credential-integration-{Guid.NewGuid():N}");

        return client;
    }

    private sealed class TestResetNotifier : ICredentialResetNotifier
    {
        private readonly ConcurrentQueue<CredentialResetNotification>
            _notifications = new();

        public IReadOnlyList<CredentialResetNotification> Notifications =>
            _notifications.ToArray();

        public Task NotifyAsync(
            CredentialResetNotification notification,
            CancellationToken ct = default)
        {
            _notifications.Enqueue(notification);
            return Task.CompletedTask;
        }
    }


    private static AuthServerFactory CreateFactory(TestResetNotifier notifier)
    {
        return AuthServerFactory.CreateWithServices(services =>
        {
            services.RemoveAll<ICredentialResetNotifier>();
            services.AddSingleton<ICredentialResetNotifier>(notifier);
        });
    }

    private static Task<HttpResponseMessage> BeginResetAsync(HttpClient client, string url, string identifier)
    {
        return client.PostAsJsonAsync(
            url,
            new BeginResetCredentialRequest
            {
                Identifier = identifier,
                CredentialType = CredentialType.Password,
                ResetCodeType = ResetCodeType.Token
            });
    }

    private static Task<HttpResponseMessage> CompleteResetAsync(HttpClient client, string url, string identifier, string token, string newPassword)
    {
        return client.PostAsJsonAsync(
            url,
            new CompleteResetCredentialRequest
            {
                Identifier = identifier,
                CredentialType = CredentialType.Password,
                ResetToken = token,
                NewSecret = newPassword
            });
    }

    private static async Task AuthenticateAsync(HttpClient client, IntegrationTestUser user)
    {
        var response = await client.PostAsJsonAsync(
            "/auth/login",
            new
            {
                identifier = user.Identifier,
                secret = user.Secret
            });

        response.StatusCode.Should().Be(HttpStatusCode.Found);

        response.Headers.TryGetValues(
            "Set-Cookie",
            out var values).Should().BeTrue();

        var cookie = values!
            .First(x => x.StartsWith(
                "uas=",
                StringComparison.OrdinalIgnoreCase))
            .Split(';', 2)[0];

        client.DefaultRequestHeaders.Add("Cookie", cookie);
    }

    private static async Task AssertPasswordAsync(AuthServerFactory factory, IntegrationTestUser user, string expectedPassword)
    {
        var credential = await GetPasswordCredentialAsync(factory, user);

        using var scope = factory.Services.CreateScope();

        var hasher = scope.ServiceProvider
            .GetRequiredService<IUAuthPasswordHasher>();

        hasher.Verify(credential.SecretHash, expectedPassword)
            .Should().BeTrue();
    }

    private static async Task AssertPasswordRejectedAsync(AuthServerFactory factory, IntegrationTestUser user, string password)
    {
        var credential = await GetPasswordCredentialAsync(factory, user);

        using var scope = factory.Services.CreateScope();

        var hasher = scope.ServiceProvider
            .GetRequiredService<IUAuthPasswordHasher>();

        hasher.Verify(credential.SecretHash, password)
            .Should().BeFalse();
    }

    private static async Task<PasswordCredential> GetPasswordCredentialAsync(AuthServerFactory factory, IntegrationTestUser user)
    {
        using var scope = factory.Services.CreateScope();

        var storeFactory = scope.ServiceProvider
            .GetRequiredService<IPasswordCredentialStoreFactory>();

        var store = storeFactory.Create(TenantKeys.Single);

        var credentials = await store.GetByUserAsync(user.UserKey);

        return credentials
            .OfType<PasswordCredential>()
            .Single();
    }
}
