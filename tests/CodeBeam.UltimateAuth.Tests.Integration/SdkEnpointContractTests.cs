using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Credentials;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Tests.Integration.Infrastructure;
using CodeBeam.UltimateAuth.Users.Contracts;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Http.Json;

namespace CodeBeam.UltimateAuth.Tests.Integration;

/// <summary>
/// Real TestServer HTTP response contracts. Does not exercise the SDK request client/mapper.
/// </summary>
public sealed class SdkEndpointContractTests
{
    [Fact]
    public async Task ChangeMyStatus_ShouldReturnTypedJson_NotEmpty200()
    {
        using var factory = new AuthServerFactory();
        var user = await factory.CreateLoginUserAsync();
        using var client = CreateClient(factory);
        await AuthenticateAsync(client, user);
        using var response = await client.PostAsJsonAsync("/auth/me/status",
            new ChangeUserStatusSelfRequest { NewStatus = SelfAssignableUserStatus.SelfSuspended });
        var result = await AssertTypedSuccessAsync<UserStatusChangeResult>(response);
        result.IsSuccess.Should().BeTrue();
        result.PreviousStatus.Should().Be(UserStatus.Active);
        result.CurrentStatus.Should().Be(UserStatus.SelfSuspended);
    }

    [Fact]
    public async Task ChangeUserStatus_ShouldReturnTypedJson_NotEmpty200()
    {
        using var factory = new AuthServerFactory();
        var admin = await factory.CreateLoginUserAsync();
        var target = await factory.CreateLoginUserAsync();
        await factory.GrantPermissionsAsync(admin.UserKey, [UAuthActions.Users.ChangeStatusAdmin]);
        using var client = CreateClient(factory);
        await AuthenticateAsync(client, admin);
        using var response = await client.PostAsJsonAsync(
            $"/auth/admin/users/{target.UserKey.Value}/status",
            new ChangeUserStatusAdminRequest { NewStatus = AdminAssignableUserStatus.Suspended });
        var result = await AssertTypedSuccessAsync<UserStatusChangeResult>(response);
        result.IsSuccess.Should().BeTrue();
        result.PreviousStatus.Should().Be(UserStatus.Active);
        result.CurrentStatus.Should().Be(UserStatus.Suspended);
    }

    [Fact]
    public async Task ChangeUserStatus_AlreadyInRequestedStatus_ShouldReturnTypedJson()
    {
        using var factory = new AuthServerFactory();
        var admin = await factory.CreateLoginUserAsync();
        var target = await factory.CreateLoginUserAsync();
        await factory.GrantPermissionsAsync(admin.UserKey, [UAuthActions.Users.ChangeStatusAdmin]);
        using var client = CreateClient(factory);
        await AuthenticateAsync(client, admin);
        var url = $"/auth/admin/users/{target.UserKey.Value}/status";
        var request = new ChangeUserStatusAdminRequest { NewStatus = AdminAssignableUserStatus.Suspended };
        using var first = await client.PostAsJsonAsync(url, request);
        (await AssertTypedSuccessAsync<UserStatusChangeResult>(first)).IsSuccess.Should().BeTrue();
        using var second = await client.PostAsJsonAsync(url, request);
        var result = await AssertTypedSuccessAsync<UserStatusChangeResult>(second);
        result.IsSuccess.Should().BeTrue();
        result.PreviousStatus.Should().Be(UserStatus.Suspended);
        result.CurrentStatus.Should().Be(UserStatus.Suspended);
    }

    [Fact]
    public async Task DeleteUser_ShouldReturnTypedJson_WithRequestedMode()
    {
        using var factory = new AuthServerFactory();
        var admin = await factory.CreateLoginUserAsync();
        var target = await factory.CreateLoginUserAsync();
        await factory.GrantPermissionsAsync(admin.UserKey, [UAuthActions.Users.DeleteAdmin]);
        using var client = CreateClient(factory);
        await AuthenticateAsync(client, admin);
        using var response = await client.PostAsJsonAsync(
            $"/auth/admin/users/{target.UserKey.Value}/delete",
            new DeleteUserRequest { Mode = DeleteMode.Soft });
        var result = await AssertTypedSuccessAsync<UserDeleteResult>(response);
        result.IsSuccess.Should().BeTrue();
        result.Mode.Should().Be(DeleteMode.Soft);
    }

    [Fact]
    public async Task AdminBeginReset_ShouldReturnTypedJson_WithoutProof()
    {
        var notifier = new CapturingResetNotifier();
        using var factory = CreateResetFactory(notifier);
        var admin = await factory.CreateLoginUserAsync();
        var target = await factory.CreateLoginUserAsync();
        await factory.GrantPermissionsAsync(admin.UserKey, [UAuthActions.Credentials.BeginResetAdmin]);
        using var client = CreateClient(factory);
        await AuthenticateAsync(client, admin);
        using var response = await client.PostAsJsonAsync(
            $"/auth/admin/users/{target.UserKey.Value}/credentials/reset/begin", BeginResetRequest(target));
        var result = await AssertTypedSuccessAsync<CredentialBeginResetResult>(response);
        result.ExpiresAt.Should().BeAfter(factory.Clock.UtcNow);
        var body = await response.Content.ReadAsStringAsync();
        var notification = notifier.Notifications.Should().ContainSingle(x => x.UserKey == target.UserKey).Subject;
        body.Should().NotContain(notification.Token, "reset proof must only be sent through the notifier");
    }

    [Fact]
    public async Task AdminCompleteReset_ShouldReturnTypedJson_AndChangeCredential()
    {
        var notifier = new CapturingResetNotifier();
        using var factory = CreateResetFactory(notifier);
        var admin = await factory.CreateLoginUserAsync();
        var target = await factory.CreateLoginUserAsync();
        await factory.GrantPermissionsAsync(admin.UserKey,
            [UAuthActions.Credentials.BeginResetAdmin, UAuthActions.Credentials.CompleteResetAdmin]);
        using var client = CreateClient(factory);
        await AuthenticateAsync(client, admin);
        var url = $"/auth/admin/users/{target.UserKey.Value}/credentials/reset";
        using var begin = await client.PostAsJsonAsync($"{url}/begin", BeginResetRequest(target));
        await AssertTypedSuccessAsync<CredentialBeginResetResult>(begin);
        var token = notifier.Notifications.Single(x => x.UserKey == target.UserKey).Token;
        var newSecret = $"Changed-{Guid.NewGuid():N}!";
        using var complete = await client.PostAsJsonAsync($"{url}/complete",
            new CompleteResetCredentialRequest
            {
                Identifier = target.Identifier,
                CredentialType = CredentialType.Password,
                ResetToken = token,
                NewSecret = newSecret
            });
        var result = await AssertTypedSuccessAsync<CredentialActionResult>(complete);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task AdminBeginReset_TargetMismatch_ShouldReturn400_AndNotNotify()
    {
        var notifier = new CapturingResetNotifier();
        using var factory = CreateResetFactory(notifier);
        var admin = await factory.CreateLoginUserAsync();
        var target = await factory.CreateLoginUserAsync();
        var other = await factory.CreateLoginUserAsync();
        await factory.GrantPermissionsAsync(admin.UserKey, [UAuthActions.Credentials.BeginResetAdmin]);
        using var client = CreateClient(factory);
        await AuthenticateAsync(client, admin);
        using var response = await client.PostAsJsonAsync(
            $"/auth/admin/users/{target.UserKey.Value}/credentials/reset/begin", BeginResetRequest(other));
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        notifier.Notifications.Should().BeEmpty();
    }

    [Fact]
    public async Task RevokeMy_UnknownCredential_ShouldNotReturnSuccessful204()
    {
        using var factory = new AuthServerFactory();
        var user = await factory.CreateLoginUserAsync();
        using var client = CreateClient(factory);
        await AuthenticateAsync(client, user);
        using var response = await client.PostAsJsonAsync("/auth/me/credentials/revoke",
            new RevokeCredentialRequest { Id = Guid.NewGuid() });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RevokeAdmin_UnknownCredential_ShouldNotReturnSuccessful204()
    {
        using var factory = new AuthServerFactory();
        var admin = await factory.CreateLoginUserAsync();
        var target = await factory.CreateLoginUserAsync();
        await factory.GrantPermissionsAsync(admin.UserKey, [UAuthActions.Credentials.RevokeAdmin]);
        using var client = CreateClient(factory);
        await AuthenticateAsync(client, admin);
        using var response = await client.PostAsJsonAsync(
            $"/auth/admin/users/{target.UserKey.Value}/credentials/revoke",
            new RevokeCredentialRequest { Id = Guid.NewGuid() });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteCredentialAdmin_UnknownCredential_ShouldNotReturnSuccessful204()
    {
        using var factory = new AuthServerFactory();
        var admin = await factory.CreateLoginUserAsync();
        var target = await factory.CreateLoginUserAsync();
        await factory.GrantPermissionsAsync(admin.UserKey, [UAuthActions.Credentials.DeleteAdmin]);
        using var client = CreateClient(factory);
        await AuthenticateAsync(client, admin);
        using var response = await client.PostAsJsonAsync(
            $"/auth/admin/users/{target.UserKey.Value}/credentials/delete",
            new DeleteCredentialRequest { Id = Guid.NewGuid(), Mode = DeleteMode.Soft });
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static BeginResetCredentialRequest BeginResetRequest(IntegrationTestUser user) => new()
    {
        Identifier = user.Identifier,
        CredentialType = CredentialType.Password,
        ResetCodeType = ResetCodeType.Token
    };

    private static AuthServerFactory CreateResetFactory(CapturingResetNotifier notifier) =>
        AuthServerFactory.CreateWithServices(services =>
        {
            services.RemoveAll<ICredentialResetNotifier>();
            services.AddSingleton<ICredentialResetNotifier>(notifier);
        });

    private static async Task<T> AssertTypedSuccessAsync<T>(HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.OK,
            "FromJson<T> requires a successful response with JSON content");
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        var json = await response.Content.ReadAsStringAsync();
        json.Should().NotBeNullOrWhiteSpace();
        var value = await response.Content.ReadFromJsonAsync<T>();
        value.Should().NotBeNull();
        return value!;
    }

    private sealed class CapturingResetNotifier : ICredentialResetNotifier
    {
        private readonly ConcurrentQueue<CredentialResetNotification> _notifications = new();
        public IReadOnlyList<CredentialResetNotification> Notifications => _notifications.ToArray();
        public Task NotifyAsync(CredentialResetNotification notification, CancellationToken ct = default)
        {
            _notifications.Enqueue(notification);
            return Task.CompletedTask;
        }
    }

    private static HttpClient CreateClient(AuthServerFactory factory)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });
        client.DefaultRequestHeaders.Add("Origin", "https://localhost:6130");
        client.DefaultRequestHeaders.Add("X-UDID", $"contract-{Guid.NewGuid():N}");
        return client;
    }

    private static async Task AuthenticateAsync(HttpClient client, IntegrationTestUser user)
    {
        using var response = await client.PostAsJsonAsync("/auth/login", new
        {
            identifier = user.Identifier,
            secret = user.Secret
        });
        response.StatusCode.Should().Be(HttpStatusCode.Found);
        response.Headers.TryGetValues("Set-Cookie", out var values).Should().BeTrue();
        var cookie = values!.First(x => x.StartsWith("uas=", StringComparison.OrdinalIgnoreCase)).Split(';', 2)[0];
        client.DefaultRequestHeaders.Add("Cookie", cookie);
    }
}
