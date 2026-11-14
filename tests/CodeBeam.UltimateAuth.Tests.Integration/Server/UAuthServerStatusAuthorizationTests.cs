using AwesomeAssertions;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Runtime;
using CodeBeam.UltimateAuth.Server.Authorization;
using CodeBeam.UltimateAuth.Server.Runtime;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace CodeBeam.UltimateAuth.Tests.Integration.Server;

public sealed class UAuthServerStatusAuthorizationTests
{
    private static readonly UserKey User = UserKey.FromString("user-1");

    [Theory]
    [InlineData(true, true, true, false, HttpStatusCode.OK)]
    [InlineData(true, false, true, false, HttpStatusCode.Forbidden)]
    [InlineData(true, false, false, false, HttpStatusCode.Forbidden)]
    [InlineData(false, false, false, false, HttpStatusCode.Forbidden)]
    [InlineData(true, true, true, true, HttpStatusCode.Forbidden)]
    public async Task ProtectedEndpoint_ShouldEnforceRuntimeUserState(
        bool exists,
        bool isActive,
        bool canAuthenticate,
        bool isDeleted,
        HttpStatusCode expected)
    {
        await using var host = await CreateHostAsync(
            CreateState(exists, isActive, canAuthenticate, isDeleted));

        using var response = await host.Client.GetAsync(
            "/protected",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(expected);
    }

    [Fact]
    public async Task ProtectedEndpoint_WhenUserStateIsMissing_ShouldForbid()
    {
        await using var host = await CreateHostAsync(null);

        using var response = await host.Client.GetAsync(
            "/protected",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData(true, false, true, false, HttpStatusCode.OK)]
    [InlineData(true, true, true, false, HttpStatusCode.OK)]
    [InlineData(true, false, false, false, HttpStatusCode.Forbidden)]
    [InlineData(false, false, true, false, HttpStatusCode.Forbidden)]
    [InlineData(true, false, true, true, HttpStatusCode.Forbidden)]
    public async Task AllowInactive_ShouldRespectAuthenticationEligibility(
        bool exists,
        bool isActive,
        bool canAuthenticate,
        bool isDeleted,
        HttpStatusCode expected)
    {
        await using var host = await CreateHostAsync(
            CreateState(exists, isActive, canAuthenticate, isDeleted));

        using var response = await host.Client.GetAsync(
            "/support",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(expected);
    }

    [Fact]
    public async Task SuspendedAdmin_ShouldNotAccessNormalAdminEndpoint()
    {
        await using var host = await CreateHostAsync(
            CreateState(isActive: false));

        using var response = await host.Client.GetAsync(
            "/admin",
            TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AllowInactive_ShouldNotBypassRoleRequirement()
    {
        await using var host = await CreateHostAsync(
            CreateState(isActive: false));

        using var response = await host.Client.GetAsync(
            "/superadmin-support",
            TestContext.Current.CancellationToken);

        // Authenticated user has Admin, not SuperAdmin.
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AllowInactive_ShouldPreserveNamedPolicy()
    {
        await using var host = await CreateHostAsync(
            CreateState(isActive: false));

        using var response = await host.Client.GetAsync(
            "/policy-support",
            TestContext.Current.CancellationToken);

        // Named policy requires the Reports.Read claim.
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AnonymousRequest_ShouldRemainUnauthorized()
    {
        await using var host = await CreateHostAsync(
            CreateState());

        using var request = new HttpRequestMessage(
            HttpMethod.Get, "/support");

        host.Client.DefaultRequestHeaders.Remove("X-Test-Auth");

        using var response = await host.Client.SendAsync(
            request, TestContext.Current.CancellationToken);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ExistingSession_ShouldReflectUserStateChanges()
    {
        await using var host = await CreateHostAsync(CreateState());

        using var active = await host.Client.GetAsync(
            "/protected", TestContext.Current.CancellationToken);

        active.StatusCode.Should().Be(HttpStatusCode.OK);

        host.State = CreateState(isActive: false);

        using var suspended = await host.Client.GetAsync(
            "/protected", TestContext.Current.CancellationToken);

        suspended.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        host.State = CreateState();

        using var restored = await host.Client.GetAsync(
            "/protected", TestContext.Current.CancellationToken);

        restored.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private static UserRuntimeRecord CreateState(
        bool exists = true,
        bool isActive = true,
        bool canAuthenticate = true,
        bool isDeleted = false)
        => new()
        {
            UserKey = User,
            Exists = exists,
            IsActive = isActive,
            CanAuthenticate = canAuthenticate,
            IsDeleted = isDeleted
        };

    private static async Task<TestHost> CreateHostAsync(
        UserRuntimeRecord? state)
    {
        var builder = WebApplication.CreateBuilder();

        builder.WebHost.UseTestServer();

        var holder = new UserStateHolder { State = state };

        var provider = new Mock<IUserRuntimeStateProvider>();

        provider.Setup(x => x.GetAsync(
                It.IsAny<TenantKey>(),
                It.IsAny<UserKey>(),
                It.IsAny<CancellationToken>()))
            .Returns((TenantKey tenant, UserKey userKey, CancellationToken ct) =>
                Task.FromResult(holder.State));

        builder.Services.AddSingleton(provider.Object);
        builder.Services.AddSingleton<IUAuthRuntimeMarker, ServerRuntimeMarker>();

        builder.Services
            .AddAuthentication(UAuthConstants.SchemeDefaults.GlobalScheme)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                UAuthConstants.SchemeDefaults.GlobalScheme, _ => { });

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("Reports.Read", policy =>
                policy.RequireClaim("reports", "read"));
        });

        builder.Services.AddSingleton<
            IAuthorizationMiddlewareResultHandler,
            UAuthStatusAuthorizationResultHandler>();

        var app = builder.Build();

        app.UseRouting();

        // Same tenant context contract as TenantMiddleware.
        app.Use(async (context, next) =>
        {
            context.Items[UAuthConstants.HttpItems.TenantContextKey] =
                UAuthTenantContext.SingleTenant();

            await next();
        });

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapGet("/protected", () => Results.Ok())
            .RequireAuthorization();

        app.MapGet("/admin", () => Results.Ok())
            .RequireAuthorization(
                new AuthorizeAttribute { Roles = "Admin" });

        app.MapGet("/support", () => Results.Ok())
            .WithMetadata(new UAuthAllowInactiveAttribute())
            .RequireAuthorization();

        app.MapGet("/superadmin-support", () => Results.Ok())
            .WithMetadata(new UAuthAllowInactiveAttribute())
            .RequireAuthorization(
                new AuthorizeAttribute { Roles = "SuperAdmin" });

        app.MapGet("/policy-support", () => Results.Ok())
            .WithMetadata(new UAuthAllowInactiveAttribute())
            .RequireAuthorization("Reports.Read");

        await app.StartAsync();

        var client = app.GetTestClient();
        client.DefaultRequestHeaders.Add("X-Test-Auth", "true");

        return new TestHost(app, client, holder);
    }

    private sealed class UserStateHolder
    {
        public UserRuntimeRecord? State { get; set; }
    }

    private sealed class TestHost(
        WebApplication app,
        HttpClient client,
        UserStateHolder holder) : IAsyncDisposable
    {
        public HttpClient Client { get; } = client;

        public UserRuntimeRecord? State
        {
            get => holder.State;
            set => holder.State = value;
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await app.StopAsync();
            await app.DisposeAsync();
        }
    }

    private sealed class TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(
            options, logger, encoder)
    {
        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.ContainsKey("X-Test-Auth"))
                return Task.FromResult(AuthenticateResult.NoResult());

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, User.Value),
                new Claim(ClaimTypes.Role, "Admin")
            };

            var identity = new ClaimsIdentity(
                claims,
                UAuthConstants.SchemeDefaults.GlobalScheme);

            var principal = new ClaimsPrincipal(identity);

            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(principal, Scheme.Name)));
        }
    }
}
