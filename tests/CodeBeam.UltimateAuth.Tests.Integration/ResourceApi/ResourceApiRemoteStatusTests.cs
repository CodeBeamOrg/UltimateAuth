using AwesomeAssertions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server;
using CodeBeam.UltimateAuth.Server.ResourceApi;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace CodeBeam.UltimateAuth.Tests.Integration.ResourceApi;

public sealed class ResourceApiRemoteStatusTests
{
    private const string SessionValue =
        "test-resource-session-000000000000000000000001";

    private static readonly TenantKey Tenant = TenantKey.Single;
    private static readonly UserKey User = UserKey.FromString("user-1");

    [Theory]
    [InlineData(UserStatus.Active, HttpStatusCode.OK)]
    [InlineData(UserStatus.Suspended, HttpStatusCode.Forbidden)]
    [InlineData(UserStatus.SelfSuspended, HttpStatusCode.Forbidden)]
    [InlineData(UserStatus.Disabled, HttpStatusCode.Forbidden)]
    [InlineData(UserStatus.Unknown, HttpStatusCode.Forbidden)]
    public async Task ProtectedEndpoint_ShouldEnforceHubUserStatus(
        UserStatus status,
        HttpStatusCode expected)
    {
        var hub = new FakeHubHandler { UserStatus = status };

        await using var host = await CreateHostAsync(hub);

        AddSession(host.Client);

        using var response = await host.Client.GetAsync(
            "/__tests/resource/authenticated",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(expected);

        hub.RequestCount.Should().BeGreaterThan(0);
    }

    [Theory]
    [InlineData(UserStatus.Active, HttpStatusCode.OK)]
    [InlineData(UserStatus.Suspended, HttpStatusCode.OK)]
    [InlineData(UserStatus.SelfSuspended, HttpStatusCode.OK)]
    [InlineData(UserStatus.Disabled, HttpStatusCode.Forbidden)]
    [InlineData(UserStatus.Unknown, HttpStatusCode.Forbidden)]
    public async Task SupportTicket_ShouldRespectHubUserStatus(
        UserStatus status,
        HttpStatusCode expected)
    {
        var hub = new FakeHubHandler { UserStatus = status };

        await using var host = await CreateHostAsync(hub);

        AddSession(host.Client);

        using var response = await host.Client.PostAsync(
            "/__tests/resource/support/tickets",
            content: null,
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(expected);
    }

    [Fact]
    public async Task ExistingSession_ShouldReflectHubStatusChanges()
    {
        var hub = new FakeHubHandler
        {
            UserStatus = UserStatus.Active
        };

        await using var host = await CreateHostAsync(hub);

        AddSession(host.Client);

        using var active = await host.Client.GetAsync(
            "/__tests/resource/authenticated",
            TestContext.Current.CancellationToken);

        active.StatusCode.Should().Be(HttpStatusCode.OK);

        hub.UserStatus = UserStatus.Suspended;

        using var suspended = await host.Client.GetAsync(
            "/__tests/resource/authenticated",
            TestContext.Current.CancellationToken);

        suspended.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        hub.UserStatus = UserStatus.Active;

        using var restored = await host.Client.GetAsync(
            "/__tests/resource/authenticated",
            TestContext.Current.CancellationToken);

        restored.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task RevokedSession_ShouldReturnUnauthorized()
    {
        var hub = new FakeHubHandler
        {
            SessionState = SessionState.Revoked
        };

        await using var host = await CreateHostAsync(hub);

        AddSession(host.Client);

        using var response = await host.Client.GetAsync(
            "/__tests/resource/authenticated",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private static Task<ResourceApiTestHost> CreateHostAsync(
        FakeHubHandler hub)
    {
        return ResourceApiTestHost.CreateAsync(
            new Moq.Mock<ISessionValidator>().Object,
            configureServices: services =>
            {
                // Replace the test host's mocked validator with
                // the real Resource API remote validator.
                services.RemoveAll<ISessionValidator>();

                services
                    .AddHttpClient<ISessionValidator, RemoteSessionValidator>(
                        client =>
                        {
                            client.BaseAddress =
                                new Uri("https://uauth.test");
                        })
                    .ConfigurePrimaryHttpMessageHandler(() => hub);
            });
    }

    private static void AddSession(HttpClient client)
    {
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue(
                "Bearer", SessionValue);
    }

    private sealed class FakeHubHandler : HttpMessageHandler
    {
        public UserStatus UserStatus { get; set; } =
            UserStatus.Active;

        public SessionState SessionState { get; set; } =
            SessionState.Active;

        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;

            if (request.Method != HttpMethod.Post ||
                request.RequestUri?.AbsolutePath != "/auth/validate")
            {
                return Task.FromResult(
                    new HttpResponseMessage(HttpStatusCode.NotFound));
            }

            var dto = new AuthValidationResult
            {
                State = SessionState,
                ChainId = SessionChainId.New(),
                RootId = SessionRootId.New(),
                Snapshot = new AuthStateSnapshot
                {
                    Identity = new AuthIdentitySnapshot
                    {
                        Tenant = Tenant,
                        UserKey = User,
                        UserStatus = UserStatus,
                        AuthenticatedAt = DateTimeOffset.UtcNow
                    },
                    Claims = ClaimsSnapshot.Empty
                }
            };

            return Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = JsonContent.Create(dto)
                });
        }
    }
}
