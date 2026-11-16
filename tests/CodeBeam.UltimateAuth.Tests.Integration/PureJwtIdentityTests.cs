using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.InMemory;
using CodeBeam.UltimateAuth.Server;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Contracts;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;
using Moq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Security.Cryptography;

namespace CodeBeam.UltimateAuth.Tests.Integration;

public sealed class PureJwtIdentityTests
{
    private const string Issuer = "ultimateauth-integration";
    private const string Audience = "ultimateauth-integration-api";
    private const string KeyId = "purejwt-integration-key";
    private const string TestDeviceId = "purejwt-integration-device-0000000123456789";

    private sealed record IdentityResponse(
        bool PrincipalAuthenticated,
        string? PrincipalSubject,
        bool CurrentUserAuthenticated,
        string? CurrentUserKey,
        bool FlowAuthenticated,
        string? FlowUserKey,
        UAuthMode Mode,
        bool HasSession);

    [Fact]
    public async Task Valid_jwt_propagates_identity_without_session_validation()
    {
        await using var host = await Host.CreateAsync();

        var user = UserKey.New();
        var token = host.IssueToken(user);

        using var response = await host.GetAsync(token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<IdentityResponse>(
                cancellationToken:
                    TestContext.Current.CancellationToken);

        Assert.NotNull(result);

        Assert.True(result.PrincipalAuthenticated);
        Assert.True(result.CurrentUserAuthenticated);
        Assert.True(result.FlowAuthenticated);

        Assert.Equal(user.Value, result.PrincipalSubject);
        Assert.Equal(user.Value, result.CurrentUserKey);
        Assert.Equal(user.Value, result.FlowUserKey);

        Assert.Equal(UAuthMode.PureJwt, result.Mode);
        Assert.False(result.HasSession);

        host.SessionValidator.Verify(
            x => x.ValidateSessionAsync(
                It.IsAny<SessionValidationContext>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        host.RuntimeStateProvider.Verify(
            x => x.GetAsync(
                It.IsAny<TenantKey>(),
                It.IsAny<UserKey>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Valid_jwt_can_access_protected_endpoint_without_runtime_user_state()
    {
        await using var host = await Host.CreateAsync();

        var user = UserKey.New();
        var token = host.IssueToken(user);

        using var response = await host.GetAsync(
            token,
            "/test/purejwt/protected");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        host.RuntimeStateProvider.Verify(
            x => x.GetAsync(
                It.IsAny<TenantKey>(),
                It.IsAny<UserKey>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Missing_jwt_is_unauthorized()
    {
        await using var host = await Host.CreateAsync();

        using var response = await host.GetAsync(path: "/test/purejwt/protected");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Invalid_jwt_is_unauthorized()
    {
        await using var host = await Host.CreateAsync();

        using var response = await host.GetAsync(
            "not-a-valid-jwt",
            "/test/purejwt/protected");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Foreign_tenant_jwt_is_unauthorized()
    {
        await using var host = await Host.CreateAsync();

        var token = host.IssueToken(
            UserKey.New(),
            TenantKey.FromExternal("foreign-tenant"));

        using var response = await host.GetAsync(token, "/test/purejwt/protected");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Expired_jwt_is_unauthorized()
    {
        await using var host = await Host.CreateAsync();

        var token = host.IssueToken(
            UserKey.New(),
            issuedAt: DateTimeOffset.UtcNow.AddHours(-2),
            expiresAt: DateTimeOffset.UtcNow.AddHours(-1));

        using var response = await host.GetAsync(token, "/test/purejwt/protected");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Session_bound_jwt_is_unauthorized()
    {
        await using var host = await Host.CreateAsync();

        var token = host.IssueToken(
            UserKey.New(),
            extraClaims: new Dictionary<string, object>
            {
                ["sid"] = "session-123"
            });

        using var response = await host.GetAsync(token, "/test/purejwt/protected");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }


    [Fact]
    public async Task Valid_jwt_for_nonexistent_user_is_accepted_without_runtime_lookup()
    {
        await using var host = await Host.CreateAsync();

        var user = UserKey.New();
        var token = host.IssueToken(user);

        using var response = await host.GetAsync(
            token,
            "/test/purejwt/protected");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        host.RuntimeStateProvider.Verify(
            x => x.GetAsync(
                It.IsAny<TenantKey>(),
                It.IsAny<UserKey>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Valid_jwt_remains_accepted_after_runtime_user_deletion()
    {
        await using var host = await Host.CreateAsync();

        var user = UserKey.New();

        host.SetUserState(user);

        var token = host.IssueToken(user);

        host.SetUserState(
            user,
            isActive: false,
            canAuthenticate: false,
            isDeleted: true);

        using var response = await host.GetAsync(
            token,
            "/test/purejwt/protected");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        host.RuntimeStateProvider.Verify(
            x => x.GetAsync(
                It.IsAny<TenantKey>(),
                It.IsAny<UserKey>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Valid_jwt_is_independent_of_runtime_user_state_changes()
    {
        await using var host = await Host.CreateAsync();

        var user = UserKey.New();
        host.SetUserState(user);

        var token = host.IssueToken(user);

        using (var active = await host.GetAsync(
            token, "/test/purejwt/protected"))
        {
            Assert.Equal(HttpStatusCode.OK, active.StatusCode);
        }

        host.SetUserState(
            user,
            isActive: false,
            canAuthenticate: true);

        using (var suspended = await host.GetAsync(
            token, "/test/purejwt/protected"))
        {
            Assert.Equal(HttpStatusCode.OK, suspended.StatusCode);
        }

        host.SetUserState(user);

        using (var restored = await host.GetAsync(
            token, "/test/purejwt/protected"))
        {
            Assert.Equal(HttpStatusCode.OK, restored.StatusCode);
        }

        host.RuntimeStateProvider.Verify(
            x => x.GetAsync(
                It.IsAny<TenantKey>(),
                It.IsAny<UserKey>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Valid_jwt_without_request_device_id_is_unauthorized()
    {
        await using var host = await Host.CreateAsync();

        var token = host.IssueToken(UserKey.New());

        using var response = await host.GetAsync(
            token,
            "/test/purejwt/protected",
            deviceId: null);

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Valid_jwt_with_mismatched_device_id_is_unauthorized()
    {
        await using var host = await Host.CreateAsync();

        var token = host.IssueToken(UserKey.New());

        using var response = await host.GetAsync(
            token,
            "/test/purejwt/protected",
            deviceId: "different-device-0000000123456789");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task Jwt_without_device_id_claim_is_unauthorized()
    {
        await using var host = await Host.CreateAsync();

        var token = host.IssueToken(
            UserKey.New(),
            includeDeviceId: false);

        using var response = await host.GetAsync(
            token,
            "/test/purejwt/protected");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);
    }

    [Fact]
    public async Task PureJwt_with_AllowAndRebind_device_behavior_is_rejected()
    {
        await using var host = await Host.CreateAsync(
            DeviceMismatchBehavior.AllowAndRebind);

        var token = host.IssueToken(UserKey.New());

        using var response = await host.GetAsync(
            token,
            "/test/purejwt/protected");

        Assert.Equal(
            HttpStatusCode.Unauthorized,
            response.StatusCode);

        host.SessionValidator.Verify(
            x => x.ValidateSessionAsync(
                It.IsAny<SessionValidationContext>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PureJwt_with_Allow_device_behavior_accepts_different_request_device()
    {
        await using var host = await Host.CreateAsync(
            DeviceMismatchBehavior.Allow);

        var token = host.IssueToken(UserKey.New());

        using var response = await host.GetAsync(
            token,
            "/test/purejwt/protected",
            deviceId: "different-device-0000000123456789");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        host.SessionValidator.Verify(
            x => x.ValidateSessionAsync(
                It.IsAny<SessionValidationContext>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        host.RuntimeStateProvider.Verify(
            x => x.GetAsync(
                It.IsAny<TenantKey>(),
                It.IsAny<UserKey>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PureJwt_role_authorization_requires_admin_role()
    {
        await using var host = await Host.CreateAsync();

        var user = UserKey.New();

        var regularToken = host.IssueToken(user);

        using (var forbidden = await host.GetAsync(
            regularToken, "/test/purejwt/admin"))
        {
            Assert.Equal(
                HttpStatusCode.Forbidden,
                forbidden.StatusCode);
        }

        var adminToken = host.IssueToken(
            user,
            extraClaims: new Dictionary<string, object>
            {
                [ClaimTypes.Role] = "Admin"
            });

        using (var allowed = await host.GetAsync(
            adminToken, "/test/purejwt/admin"))
        {
            Assert.Equal(
                HttpStatusCode.OK,
                allowed.StatusCode);
        }

        host.RuntimeStateProvider.Verify(
            x => x.GetAsync(
                It.IsAny<TenantKey>(),
                It.IsAny<UserKey>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task PureJwt_claim_policy_requires_matching_permission()
    {
        await using var host = await Host.CreateAsync();

        var user = UserKey.New();

        var withoutPermission = host.IssueToken(user);

        using (var forbidden = await host.GetAsync(
            withoutPermission, "/test/purejwt/permission"))
        {
            Assert.Equal(
                HttpStatusCode.Forbidden,
                forbidden.StatusCode);
        }

        var withPermission = host.IssueToken(
            user,
            extraClaims: new Dictionary<string, object>
            {
                ["permission"] = "reports.read"
            });

        using (var allowed = await host.GetAsync(
            withPermission, "/test/purejwt/permission"))
        {
            Assert.Equal(
                HttpStatusCode.OK,
                allowed.StatusCode);
        }

        host.RuntimeStateProvider.Verify(
            x => x.GetAsync(
                It.IsAny<TenantKey>(),
                It.IsAny<UserKey>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("not-a-valid-jwt")]
    public async Task Invalid_or_missing_jwt_does_not_populate_identity(string? token)
    {
        await using var host = await Host.CreateAsync();

        using var response = await host.GetAsync(token);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var result = await response.Content
            .ReadFromJsonAsync<IdentityResponse>(
                cancellationToken:
                    TestContext.Current.CancellationToken);

        Assert.NotNull(result);

        Assert.False(result.PrincipalAuthenticated);
        Assert.False(result.CurrentUserAuthenticated);
        Assert.False(result.FlowAuthenticated);

        Assert.Null(result.PrincipalSubject);
        Assert.Null(result.CurrentUserKey);
        Assert.Null(result.FlowUserKey);
        Assert.False(result.HasSession);
    }


    private sealed class Host : IAsyncDisposable
    {
        private readonly WebApplication _app;
        private readonly HttpClient _client;
        private readonly SymmetricSecurityKey _key;
        private readonly UserStateHolder _userStates;

        public void SetUserState(
            UserKey user,
            bool isActive = true,
            bool canAuthenticate = true,
            bool isDeleted = false)
            => _userStates.Set(
                user, isActive, canAuthenticate, isDeleted);

        public void RemoveUserState(UserKey user)
            => _userStates.Remove(user);

        public Mock<ISessionValidator> SessionValidator { get; }
        public Mock<IUserRuntimeStateProvider> RuntimeStateProvider { get; }

        private Host(
            WebApplication app,
            SymmetricSecurityKey key,
            UserStateHolder userStates,
            Mock<ISessionValidator> sessionValidator,
            Mock<IUserRuntimeStateProvider> runtimeStateProvider)
        {
            _app = app;
            _client = app.GetTestClient();
            _key = key;
            _userStates = userStates;
            SessionValidator = sessionValidator;
            RuntimeStateProvider = runtimeStateProvider;
        }

        public static async Task<Host> CreateAsync(DeviceMismatchBehavior deviceMismatchBehavior = DeviceMismatchBehavior.Reject)
        {
            var builder = WebApplication.CreateBuilder(
                new WebApplicationOptions
                {
                    EnvironmentName = "Production"
                });

            builder.WebHost.UseTestServer();

            var userStates = new UserStateHolder();

            var runtimeStateProvider = new Mock<IUserRuntimeStateProvider>(MockBehavior.Strict);

            runtimeStateProvider
                .Setup(x => x.GetAsync(
                    It.IsAny<TenantKey>(),
                    It.IsAny<UserKey>(),
                    It.IsAny<CancellationToken>()))
                .Returns((TenantKey tenant, UserKey user, CancellationToken ct) =>
                    Task.FromResult(userStates.Get(user)));

            builder.Services
                .AddUltimateAuthServer(options =>
                {
                    options.AuthMode = UAuthMode.PureJwt;
                    options.AllowedModes = [UAuthMode.PureJwt];

                    options.Token.Issuer = Issuer;
                    options.Token.Audience = Audience;
                    options.Token.KeyId = KeyId;

                    options.Session.DeviceMismatchBehavior = deviceMismatchBehavior;
                })
                .AddUltimateAuthInMemory();

            builder.Services.Replace(ServiceDescriptor.Scoped<IUserRuntimeStateProvider>(_ => runtimeStateProvider.Object));

            var key = new SymmetricSecurityKey(RandomNumberGenerator.GetBytes(32))
            {
                KeyId = KeyId
            };

            builder.Services.AddSingleton<IJwtSigningKeyProvider>(
                new TestSigningKeyProvider(key));

            builder.Services.AddSingleton<IJwtVerificationKeyProvider>(
                new StaticJwtVerificationKeyProvider([key]));

            var sessionValidator =
                new Mock<ISessionValidator>(MockBehavior.Strict);

            builder.Services.Replace(
                ServiceDescriptor.Scoped<ISessionValidator>(
                    _ => sessionValidator.Object));

            var app = builder.Build();

            app.UseRouting();
            app.UseUltimateAuthWithAspNetCore(
                useRateLimiting: false);

            app.MapGet("/test/purejwt/identity",
                async (
                    HttpContext http,
                    ICurrentUser currentUser,
                    IAuthFlowContextFactory flowFactory) =>
                {
                    var flow = await flowFactory.CreateAsync(
                        http,
                        AuthFlowType.ApiAccess,
                        http.RequestAborted);

                    return Results.Json(new IdentityResponse(
                        http.User.Identity?.IsAuthenticated == true,
                        http.User.FindFirstValue(
                            ClaimTypes.NameIdentifier),
                        currentUser.IsAuthenticated,
                        currentUser.IsAuthenticated
                            ? currentUser.UserKey.Value
                            : null,
                        flow.IsAuthenticated,
                        flow.UserKey?.Value,
                        flow.EffectiveMode,
                        flow.Session is not null));
                });

            app.MapGet("/test/purejwt/protected",
                (HttpContext context) =>
                    Results.Ok(new
                    {
                        Subject = context.User.FindFirstValue(
                            ClaimTypes.NameIdentifier)
                    }))
                .RequireAuthorization();

            app.MapGet("/test/purejwt/admin",
                () => Results.Ok())
                .RequireAuthorization(
                    new AuthorizeAttribute
                    {
                        Roles = "Admin"
                    });

            app.MapGet("/test/purejwt/permission",
                () => Results.Ok())
                .RequireAuthorization(
                    new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .RequireClaim("permission", "reports.read")
                        .Build());

            await app.StartAsync();

            return new Host(app, key, userStates, sessionValidator, runtimeStateProvider);
        }

        public string IssueToken(
            UserKey user,
            TenantKey? tenant = null,
            DateTimeOffset? issuedAt = null,
            DateTimeOffset? expiresAt = null,
            IReadOnlyDictionary<string, object>? extraClaims = null,
            bool includeDeviceId = true)
        {
            var now = issuedAt ?? DateTimeOffset.UtcNow;

            using var scope = _app.Services.CreateScope();

            var generator = scope.ServiceProvider
                .GetRequiredService<IJwtTokenGenerator>();

            return generator.CreateToken(
                new UAuthJwtTokenDescriptor
                {
                    Subject = user.Value,
                    Tenant = tenant ?? TenantKey.Single,
                    Issuer = Issuer,
                    Audience = Audience,
                    KeyId = _key.KeyId,
                    IssuedAt = now,
                    ExpiresAt = expiresAt.HasValue ? expiresAt.Value.AddMinutes(10) : now.AddMinutes(10),
                    DeviceId = includeDeviceId
                        ? DeviceId.Create(TestDeviceId)
                        : null,
                    Claims = extraClaims
                });
        }


        public async Task<HttpResponseMessage> GetAsync(string? token = null, string path = "/test/purejwt/identity", string? deviceId = TestDeviceId)
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, path);

            if (deviceId is not null)
            {
                request.Headers.Add("X-UDID", deviceId);
            }

            if (token is not null)
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return await _client.SendAsync(request, TestContext.Current.CancellationToken);
        }

        public async ValueTask DisposeAsync()
        {
            _client.Dispose();
            await _app.DisposeAsync();
        }
    }

    private sealed class TestSigningKeyProvider(SymmetricSecurityKey key) : IJwtSigningKeyProvider
    {
        public JwtSigningKey Resolve(string? keyId)
        {
            if (keyId is not null &&
                !string.Equals(
                    keyId,
                    key.KeyId,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Unknown signing key: {keyId}");
            }

            return new JwtSigningKey
            {
                KeyId = key.KeyId!,
                Algorithm = SecurityAlgorithms.HmacSha256,
                Key = key
            };
        }
    }


    private sealed class UserStateHolder
    {
        private readonly Dictionary<UserKey, UserRuntimeRecord> _states = new();

        public void Set(
            UserKey user,
            bool isActive = true,
            bool canAuthenticate = true,
            bool isDeleted = false)
        {
            _states[user] = new UserRuntimeRecord
            {
                UserKey = user,
                Exists = true,
                IsActive = isActive,
                CanAuthenticate = canAuthenticate,
                IsDeleted = isDeleted
            };
        }

        public void Remove(UserKey user)
            => _states.Remove(user);

        public UserRuntimeRecord? Get(UserKey user)
            => _states.TryGetValue(user, out var state)
                ? state
                : null;
    }
}
