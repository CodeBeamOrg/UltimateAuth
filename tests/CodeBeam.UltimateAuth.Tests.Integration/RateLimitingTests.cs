using System.Net;
using System.Threading.RateLimiting;
using CodeBeam.UltimateAuth.InMemory;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Server.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Tests.Integration;

public class RateLimitingTests
{
    [Fact]
    public async Task LoginAliasesShareBudget_AndMaintenanceIsIndependent()
    {
        await using var host = await Host.Create(o => o.RateLimiting.Login.PermitLimit = 1);
        Assert.Equal(HttpStatusCode.NoContent, (await host.Post("/auth/login")).StatusCode);
        foreach (var route in new[] { "/auth/try-login", "/auth/pkce/complete", "/auth/pkce/try-complete" })
        {
            var response = await host.Post(route);
            Assert.Equal(HttpStatusCode.TooManyRequests, response.StatusCode);
            if (response.Headers.RetryAfter is not null)
                Assert.True(response.Headers.RetryAfter.Delta > TimeSpan.Zero);
            Assert.True(response.Headers.CacheControl?.NoStore);
            Assert.Contains("rate_limited", await response.Content.ReadAsStringAsync());
        }
        foreach (var route in new[] { "/auth/validate", "/auth/refresh", "/auth/logout" })
            Assert.Equal(HttpStatusCode.NoContent, (await host.Post(route)).StatusCode);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    public async Task HelperOptInAndOptOut(bool aspNet, bool enabled, bool throttled)
    {
        await using var host = await Host.Create(o => o.RateLimiting.Login.PermitLimit = 1, aspNet, enabled);
        await host.Post("/auth/login");
        Assert.Equal(throttled ? HttpStatusCode.TooManyRequests : HttpStatusCode.NoContent,
            (await host.Post("/auth/login")).StatusCode);
    }

    [Fact]
    public async Task DefaultHelperAndRepeatedHelper_DoNotConsumeTwice()
    {
        await using var host = await Host.Create(o => o.RateLimiting.Login.PermitLimit = 2, repeat: true);
        Assert.Equal(HttpStatusCode.NoContent, (await host.Post("/auth/login")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await host.Post("/auth/login")).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await host.Post("/auth/login")).StatusCode);
    }

    [Fact]
    public async Task DisableAndEndpointOverride_DoNotDisableApplicationGlobalLimiter()
    {
        await using var host = await Host.Create(o => o.RateLimiting.Enabled = false, native: o =>
        {
            o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
                RateLimitPartition.GetSlidingWindowLimiter("global", _ => new()
                { PermitLimit = 1, Window = TimeSpan.FromMinutes(1), SegmentsPerWindow = 6 }));
            o.RejectionStatusCode = 418;
        });
        await host.Post("/auth/login");
        Assert.Equal(418, (int)(await host.Post("/auth/login")).StatusCode);
    }

    [Fact]
    public async Task ApplicationGroupPolicyWins_AndGenericNamesDoNotCollide()
    {
        await using var host = await Host.Create(o => o.RateLimiting.Login.PermitLimit = 1,
            native: o => o.AddFixedWindowLimiter("Sensitive", x =>
            { x.PermitLimit = 3; x.Window = TimeSpan.FromMinutes(1); }), applicationPolicy: true);
        for (var i = 0; i < 3; i++)
            Assert.Equal(HttpStatusCode.NoContent, (await host.Post("/auth/login")).StatusCode);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, (await host.Post("/auth/login")).StatusCode);
    }

    [Fact]
    public async Task EndpointOverrideAndAnonymousIpIsolation()
    {
        await using var host = await Host.Create(o => o.RateLimiting.Endpoints[UAuthEndpoint.Login] = new() { PermitLimit = 1 });
        await host.Post("/auth/login");
        Assert.Equal(HttpStatusCode.TooManyRequests, (await host.Post("/auth/login", forwarded: "203.0.113.9")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await host.Post("/auth/login", ip: "192.0.2.2")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await host.Post("/auth/try-login")).StatusCode);
    }

    [Fact]
    public async Task EveryMappedEndpointHasOneClassification()
    {
        await using var host = await Host.Create();
        var routes = ((IEndpointRouteBuilder)host.App).DataSources.SelectMany(s => s.Endpoints).ToArray();
        // Reviewed route/identity/group contract, independent of the runtime classification lookup.
        const string expected = """
/auth/admin/authorization/roles/create|CreateRole|Standard
/auth/admin/authorization/roles/query|QueryRoles|Standard
/auth/admin/authorization/roles/{roleId}/delete|DeleteRole|Standard
/auth/admin/authorization/roles/{roleId}/permissions|SetRolePermissions|Standard
/auth/admin/authorization/roles/{roleId}/rename|RenameRole|Standard
/auth/admin/authorization/users/{userKey}/roles/assign|AssignRole|Standard
/auth/admin/authorization/users/{userKey}/roles/get|GetUserRoles|Standard
/auth/admin/authorization/users/{userKey}/roles/remove|RemoveRole|Standard
/auth/admin/users/create|UserCreateAdmin|Standard
/auth/admin/users/get-by-keys|GetUsersByKeys|Standard
/auth/admin/users/query|QueryUsers|Standard
/auth/admin/users/{userKey}/credentials/add|CredentialAddAdmin|Standard
/auth/admin/users/{userKey}/credentials/change|CredentialChangeAdmin|Standard
/auth/admin/users/{userKey}/credentials/delete|CredentialDeleteAdmin|Standard
/auth/admin/users/{userKey}/credentials/reset/begin|CredentialResetBeginAdmin|Sensitive
/auth/admin/users/{userKey}/credentials/reset/complete|CredentialResetCompleteAdmin|Sensitive
/auth/admin/users/{userKey}/credentials/revoke|CredentialRevokeAdmin|Standard
/auth/admin/users/{userKey}/delete|UserDeleteAdmin|Standard
/auth/admin/users/{userKey}/identifiers/add|AddUserIdentifierAdmin|Standard
/auth/admin/users/{userKey}/identifiers/delete|DeleteUserIdentifierAdmin|Standard
/auth/admin/users/{userKey}/identifiers/get|GetUserIdentifiers|Standard
/auth/admin/users/{userKey}/identifiers/set-primary|SetPrimaryUserIdentifierAdmin|Standard
/auth/admin/users/{userKey}/identifiers/unset-primary|UnsetPrimaryUserIdentifierAdmin|Standard
/auth/admin/users/{userKey}/identifiers/update|UpdateUserIdentifierAdmin|Standard
/auth/admin/users/{userKey}/identifiers/verify|VerifyUserIdentifierAdmin|Standard
/auth/admin/users/{userKey}/logout-all|LogoutAllAdmin|Standard
/auth/admin/users/{userKey}/logout-device|LogoutDeviceAdmin|Standard
/auth/admin/users/{userKey}/logout-others|LogoutOthersAdmin|Standard
/auth/admin/users/{userKey}/profile/create|CreateProfileAdmin|Standard
/auth/admin/users/{userKey}/profile/delete|DeleteProfileAdmin|Standard
/auth/admin/users/{userKey}/profile/get|ProfileGetAdmin|Standard
/auth/admin/users/{userKey}/profile/update|ProfileUpdateAdmin|Standard
/auth/admin/users/{userKey}/sessions/chains/{chainId}/revoke|RevokeUserChain|Standard
/auth/admin/users/{userKey}/sessions/chains/{chainId}|GetUserChainDetail|Standard
/auth/admin/users/{userKey}/sessions/chains|GetUserChains|Standard
/auth/admin/users/{userKey}/sessions/revoke-all|RevokeAllChains|Standard
/auth/admin/users/{userKey}/sessions/revoke-root|RevokeRoot|Standard
/auth/admin/users/{userKey}/sessions/{sessionId}/revoke|RevokeUserSession|Standard
/auth/admin/users/{userKey}/status|ChangeStatusAdmin|Standard
/auth/login|Login|Sensitive
/auth/logout|Logout|Frequent
/auth/me/authorization/check|AuthorizationCheck|Frequent
/auth/me/authorization/permissions/get|GetMyPermissions|Standard
/auth/me/authorization/roles/get|GetMyRoles|Standard
/auth/me/credentials/add|CredentialAddSelf|Sensitive
/auth/me/credentials/change|CredentialChangeSelf|Sensitive
/auth/me/credentials/reset/begin|CredentialResetBeginAnonymous|Sensitive
/auth/me/credentials/reset/complete|CredentialResetCompleteAnonymous|Sensitive
/auth/me/credentials/revoke|CredentialRevokeSelf|Standard
/auth/me/delete|UserDeleteSelf|Standard
/auth/me/identifiers/add|AddUserIdentifierSelf|Sensitive
/auth/me/identifiers/check-availability|CheckIdentifierAvailability|Sensitive
/auth/me/identifiers/delete|DeleteUserIdentifierSelf|Sensitive
/auth/me/identifiers/get|GetMyIdentifiers|Standard
/auth/me/identifiers/set-primary|SetPrimaryUserIdentifierSelf|Sensitive
/auth/me/identifiers/unset-primary|UnsetPrimaryUserIdentifierSelf|Sensitive
/auth/me/identifiers/update|UpdateUserIdentifierSelf|Sensitive
/auth/me/identifiers/verify|VerifyUserIdentifierSelf|Sensitive
/auth/me/logout-all|LogoutAllSelf|Standard
/auth/me/logout-device|LogoutDeviceSelf|Standard
/auth/me/logout-others|LogoutOthersSelf|Standard
/auth/me/profile/create|CreateProfileSelf|Standard
/auth/me/profile/delete|DeleteProfileSelf|Standard
/auth/me/profile/get|ProfileGetSelf|Standard
/auth/me/profile/update|ProfileUpdateSelf|Standard
/auth/me/sessions/chains/{chainId}/revoke|RevokeMyChain|Standard
/auth/me/sessions/chains/{chainId}|GetMyChainDetail|Standard
/auth/me/sessions/chains|GetMyChains|Standard
/auth/me/sessions/revoke-all|RevokeAllMyChains|Standard
/auth/me/sessions/revoke-others|RevokeOtherChains|Standard
/auth/me/status|ChangeStatusSelf|Standard
/auth/pkce/authorize|PkceAuthorize|Sensitive
/auth/pkce/complete|PkceComplete|Sensitive
/auth/pkce/try-complete|PkceTryComplete|Sensitive
/auth/refresh|Refresh|Frequent
/auth/try-login|TryLogin|Sensitive
/auth/uauthhub/entry|HubEntry|Sensitive
/auth/users/create|UserCreateAnonymous|Sensitive
/auth/validate|Validate|Frequent
""";
        var actual = routes.Cast<RouteEndpoint>().Select(endpoint =>
        {
            var metadata = Assert.Single(endpoint.Metadata, m => m.GetType().Name == "UAuthRateLimitMetadata");
            var type = metadata.GetType();
            Assert.Equal(new[] { "POST" }, endpoint.Metadata.GetMetadata<HttpMethodMetadata>()!.HttpMethods);
            return $"{endpoint.RoutePattern.RawText}|{type.GetProperty("Endpoint")!.GetValue(metadata)}|{type.GetProperty("Group")!.GetValue(metadata)}";
        }).OrderBy(x => x, StringComparer.Ordinal).ToArray();
        Assert.Equal(expected.Split('\n').Select(x => x.Trim()).OrderBy(x => x, StringComparer.Ordinal), actual);
    }

    [Fact]
    public async Task TrustedForwardingSeparatesClients_UnknownProxiesCannotRotateHeaders()
    {
        await using var host = await Host.Create(o => o.RateLimiting.Login.PermitLimit = 1, trustedProxy: true);
        Assert.Equal(HttpStatusCode.NoContent, (await host.Post("/auth/login", forwarded: "203.0.113.1")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await host.Post("/auth/login", forwarded: "203.0.113.2")).StatusCode);
        await host.Post("/auth/login", ip: "192.0.2.2", forwarded: "203.0.113.3");
        Assert.Equal(HttpStatusCode.TooManyRequests,
            (await host.Post("/auth/login", ip: "192.0.2.2", forwarded: "203.0.113.4")).StatusCode);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task NamedApplicationPolicyUsesEffectiveMiddlewareRejection(bool explicitOptions)
    {
        var options = new RateLimiterOptions();
        options.AddFixedWindowLimiter("application-login", r => { r.PermitLimit = 1; r.Window = TimeSpan.FromMinutes(1); });
        options.OnRejected = (rejected, ct) => { rejected.HttpContext.Response.StatusCode = 418; return ValueTask.CompletedTask; };
        await using var host = await Host.Create(o => o.RateLimiting.Login.PolicyName = "application-login",
            native: explicitOptions ? null : o =>
            {
                o.AddFixedWindowLimiter("application-login", r => { r.PermitLimit = 1; r.Window = TimeSpan.FromMinutes(1); });
                o.OnRejected = options.OnRejected;
            }, middlewareOptions: explicitOptions ? options : null);
        await host.Post("/auth/login");
        Assert.Equal(418, (int)(await host.Post("/auth/login")).StatusCode);
    }

    [Fact]
    public async Task BuiltInPolicyOwnsRejectionEvenWithExplicitMiddlewareOptions()
    {
        var options = new RateLimiterOptions { OnRejected = (_, _) => throw new InvalidOperationException("Global callback must not replace endpoint policy rejection.") };
        await using var host = await Host.Create(o => o.RateLimiting.Login.PermitLimit = 1, middlewareOptions: options);
        await host.Post("/auth/login");
        Assert.Equal(HttpStatusCode.TooManyRequests, (await host.Post("/auth/login")).StatusCode);
    }

    [Fact]
    public async Task ChildPipelineDoesNotInheritInstallation_AndDeduplicatesItsOwnCalls()
    {
        await using var host = await Host.Create(o => o.RateLimiting.Login.PermitLimit = 1);
        var endpoint = ((IEndpointRouteBuilder)host.App).DataSources.SelectMany(s => s.Endpoints)
            .OfType<RouteEndpoint>().Single(e => e.RoutePattern.RawText == "/auth/login");
        var child = ((IApplicationBuilder)host.App).New();
        child.UseUltimateAuth(useRateLimiting: true);
        child.UseUltimateAuth(useRateLimiting: true);
        child.Run(context => { context.Response.StatusCode = 204; return Task.CompletedTask; });
        var pipeline = child.Build();
        async Task<int> Send()
        {
            using var scope = host.App.Services.CreateScope();
            var context = new DefaultHttpContext { RequestServices = scope.ServiceProvider };
            context.Connection.RemoteIpAddress = IPAddress.Parse("192.0.2.1");
            context.Response.Body = new MemoryStream();
            context.SetEndpoint(endpoint);
            await pipeline(context);
            return context.Response.StatusCode;
        }
        Assert.Equal(204, await Send());
        Assert.Equal(429, await Send());
    }

    [Fact]
    public void MiddlewareHelperSignaturesAndCallFormsRemainAvailable()
    {
        var type = typeof(UltimateAuthApplicationBuilderExtensions);

        Assert.NotNull(type.GetMethod(
            "UseUltimateAuth",
            new[] { typeof(IApplicationBuilder) }));

        Assert.NotNull(type.GetMethod(
            "UseUltimateAuth",
            new[] { typeof(IApplicationBuilder), typeof(bool) }));

        var method = type.GetMethod(
            "UseUltimateAuthWithAspNetCore",
            new[]
            {
            typeof(IApplicationBuilder),
            typeof(bool?),
            typeof(bool)
            });

        Assert.NotNull(method);

        var parameters = method.GetParameters();

        Assert.True(parameters[1].IsOptional);
        Assert.Null(parameters[1].DefaultValue);

        Assert.True(parameters[2].IsOptional);
        Assert.Equal(true, parameters[2].DefaultValue);

        // Compile-time coverage for the supported public API.
        static void Calls(IApplicationBuilder app)
        {
            app.UseUltimateAuth();
            app.UseUltimateAuth(true);
            app.UseUltimateAuth(useRateLimiting: false);

            app.UseUltimateAuthWithAspNetCore();
            app.UseUltimateAuthWithAspNetCore(false);
            app.UseUltimateAuthWithAspNetCore(null);
            app.UseUltimateAuthWithAspNetCore(enableCors: true);
            app.UseUltimateAuthWithAspNetCore(null, false);
            app.UseUltimateAuthWithAspNetCore(
                enableCors: null,
                useRateLimiting: false);
        }

        Action<IApplicationBuilder> compiled = Calls;
        Assert.NotNull(compiled);
    }

    [Fact]
    public async Task NativeDisableSkipsBothGlobalAndEndpointLimits()
    {
        await using var host = await Host.Create(o => o.RateLimiting.Login.PermitLimit = 1,
            native: o => o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
                RateLimitPartition.GetFixedWindowLimiter("global", _ => new()
                { PermitLimit = 1, Window = TimeSpan.FromMinutes(1) })), disableNative: true);
        Assert.Equal(HttpStatusCode.NoContent, (await host.Post("/auth/login")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await host.Post("/auth/login")).StatusCode);
    }

    [Fact]
    public async Task GlobalAndBuiltInEndpointLimitsBothApply()
    {
        await using var host = await Host.Create(o => o.RateLimiting.Login.PermitLimit = 1, native: o =>
        {
            o.RejectionStatusCode = 418;
            o.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
                RateLimitPartition.GetFixedWindowLimiter("global", _ => new() { PermitLimit = 4, Window = TimeSpan.FromMinutes(1) }));
        });
        Assert.Equal(HttpStatusCode.NoContent, (await host.Post("/auth/login")).StatusCode);
        Assert.Equal(HttpStatusCode.TooManyRequests, (await host.Post("/auth/login")).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await host.Post("/auth/validate")).StatusCode);
        // Native middleware may acquire global permits again after endpoint rejection.
        Assert.Equal(418, (int)(await host.Post("/auth/refresh")).StatusCode);
    }

    private sealed class Host(WebApplication app) : IAsyncDisposable
    {
        internal WebApplication App => app;
        private readonly HttpClient _client = app.GetTestClient();
        internal async Task<HttpResponseMessage> Post(string path, string ip = "192.0.2.1", string? forwarded = null)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, path);
            request.Headers.Add("Test-Connection-IP", ip);
            if (forwarded is not null) request.Headers.Add("X-Forwarded-For", forwarded);
            return await _client.SendAsync(request);
        }
        internal static async Task<Host> Create(Action<UAuthServerOptions>? configure = null,
            bool aspNet = true, bool? enabled = null, bool repeat = false,
            Action<RateLimiterOptions>? native = null, bool applicationPolicy = false, bool trustedProxy = false, bool disableNative = false, RateLimiterOptions? middlewareOptions = null)
        {
            var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Production" });
            builder.WebHost.UseTestServer();
            builder.Services.AddUltimateAuthServer(configure).AddUltimateAuthInMemory();
            if (native is not null) builder.Services.AddRateLimiter(native);
            var app = builder.Build();
            app.Use((context, next) =>
            {
                // Test transport only; production never reads this header.
                context.Connection.RemoteIpAddress = IPAddress.Parse(context.Request.Headers["Test-Connection-IP"].ToString());
                return next(context);
            });
            if (trustedProxy)
            {
                var forwarding = new Microsoft.AspNetCore.Builder.ForwardedHeadersOptions
                { ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor };
                forwarding.KnownProxies.Add(IPAddress.Parse("192.0.2.1"));
                app.UseForwardedHeaders(forwarding);
            }
            app.UseRouting();
            if (middlewareOptions is not null) app.UseRateLimiter(middlewareOptions);
            if (aspNet)
            {
                if (middlewareOptions is not null) app.UseUltimateAuthWithAspNetCore(enableCors: null, useRateLimiting: false);
                else if (enabled is null) app.UseUltimateAuthWithAspNetCore();
                else app.UseUltimateAuthWithAspNetCore(enableCors: null, useRateLimiting: enabled.Value);
                if (repeat) app.UseUltimateAuthWithAspNetCore();
            }
            else app.UseUltimateAuth(useRateLimiting: enabled ?? false);
            var group = app.MapGroup("");
            if (applicationPolicy) group.RequireRateLimiting("Sensitive");
            if (disableNative) group.DisableRateLimiting();
            group.MapUltimateAuthEndpoints();
            group.MapUAuthHub();
            // Exercise the real endpoint inventory and middleware without invoking business handlers.
            ((IEndpointConventionBuilder)group).Finally(b => b.RequestDelegate = context => { context.Response.StatusCode = 204; return Task.CompletedTask; });
            await app.StartAsync();
            return new Host(app);
        }
        public async ValueTask DisposeAsync() { _client.Dispose(); await app.DisposeAsync(); }
    }
}
