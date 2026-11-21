using AwesomeAssertions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Extensions;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class ValidateCredentialResolverTests
{
    [Fact]
    public async Task Session_cookie_should_resolve_and_trim_value()
    {
        var context = CreateContext();
        context.Request.Headers.Cookie = "uauth-session=%20session-123%20";

        var resolver = CreateResolver(PrimaryTokenKind.Session);
        var response = CreateResponse(
            session: CookieDelivery(GrantKind.Session, "uauth-session"));

        var result = await resolver.ResolveAsync(context, response);

        result.Should().NotBeNull();
        result!.Kind.Should().Be(PrimaryTokenKind.Session);
        result.Value.Should().Be("session-123");
        result.Tenant.Should().Be(context.GetTenant());
        result.Device.Should().NotBeNull();
    }

    [Theory]
    [InlineData(GrantDeliveryMode.None)]
    [InlineData(GrantDeliveryMode.Header)]
    [InlineData(GrantDeliveryMode.Body)]
    public async Task Session_should_require_cookie_delivery(
        GrantDeliveryMode mode)
    {
        var context = CreateContext();
        context.Request.Headers.Cookie = "uauth-session=session-123";

        var resolver = CreateResolver(PrimaryTokenKind.Session);
        var response = CreateResponse(
            session: new EffectiveGrantDelivery(
                GrantKind.Session,
                TokenFormat.Opaque,
                mode));

        var result = await resolver.ResolveAsync(context, response);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Session_should_require_cookie_options()
    {
        var context = CreateContext();
        var resolver = CreateResolver(PrimaryTokenKind.Session);

        var response = CreateResponse(
            session: new EffectiveGrantDelivery(
                GrantKind.Session,
                TokenFormat.Opaque,
                GrantDeliveryMode.Cookie));

        var result = await resolver.ResolveAsync(context, response);

        result.Should().BeNull();
    }

    [Fact]
    public async Task Session_should_return_null_when_cookie_missing()
    {
        var context = CreateContext();
        var resolver = CreateResolver(PrimaryTokenKind.Session);

        var result = await resolver.ResolveAsync(
            context,
            CreateResponse(
                session: CookieDelivery(
                    GrantKind.Session,
                    "uauth-session")));

        result.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("   ")]
    public async Task Session_should_reject_blank_cookie(string value)
    {
        var context = CreateContext();
        context.Request.Headers.Cookie =
            $"uauth-session={Uri.EscapeDataString(value)}";

        var resolver = CreateResolver(PrimaryTokenKind.Session);

        var result = await resolver.ResolveAsync(
            context,
            CreateResponse(
                session: CookieDelivery(
                    GrantKind.Session,
                    "uauth-session")));

        result.Should().BeNull();
    }

    [Fact]
    public async Task Access_token_should_resolve_bearer_header()
    {
        var context = CreateContext();
        context.Request.Headers.Authorization = "Bearer access-123";

        var resolver = CreateResolver(PrimaryTokenKind.AccessToken);

        var result = await resolver.ResolveAsync(
            context,
            CreateResponse(access: HeaderDelivery()));

        result.Should().NotBeNull();
        result!.Kind.Should().Be(PrimaryTokenKind.AccessToken);
        result.Value.Should().Be("access-123");
        result.Tenant.Should().Be(context.GetTenant());
        result.Device.Should().NotBeNull();
    }

    [Fact]
    public async Task Access_token_should_accept_case_insensitive_bearer_prefix()
    {
        var context = CreateContext();
        context.Request.Headers.Authorization = "bEaReR access-123";

        var resolver = CreateResolver(PrimaryTokenKind.AccessToken);

        var result = await resolver.ResolveAsync(
            context,
            CreateResponse(access: HeaderDelivery()));

        result.Should().NotBeNull();
        result!.Value.Should().Be("access-123");
    }

    [Fact]
    public async Task Access_token_should_support_custom_header()
    {
        var context = CreateContext();
        context.Request.Headers["X-Access-Token"] = "access-123";

        var resolver = CreateResolver(PrimaryTokenKind.AccessToken);

        var result = await resolver.ResolveAsync(
            context,
            CreateResponse(
                access: HeaderDelivery(
                    "X-Access-Token",
                    HeaderTokenFormat.Raw)));

        result.Should().NotBeNull();
        result!.Value.Should().Be("access-123");
    }

    [Fact]
    public async Task Raw_header_should_preserve_bearer_text()
    {
        var context = CreateContext();
        context.Request.Headers.Authorization = "Bearer access-123";

        var resolver = CreateResolver(PrimaryTokenKind.AccessToken);

        var result = await resolver.ResolveAsync(
            context,
            CreateResponse(
                access: HeaderDelivery(
                    format: HeaderTokenFormat.Raw)));

        result.Should().NotBeNull();
        result!.Value.Should().Be("Bearer access-123");
    }

    [Theory]
    [InlineData(GrantDeliveryMode.None)]
    [InlineData(GrantDeliveryMode.Cookie)]
    [InlineData(GrantDeliveryMode.Body)]
    public async Task Access_token_should_require_header_delivery(
        GrantDeliveryMode mode)
    {
        var context = CreateContext();
        context.Request.Headers.Authorization = "Bearer access-123";

        var resolver = CreateResolver(PrimaryTokenKind.AccessToken);

        var result = await resolver.ResolveAsync(
            context,
            CreateResponse(
                access: new EffectiveGrantDelivery(
                    GrantKind.AccessToken,
                    TokenFormat.Jwt,
                    mode)));

        result.Should().BeNull();
    }

    [Fact]
    public async Task Access_token_should_return_null_when_header_missing()
    {
        var context = CreateContext();
        var resolver = CreateResolver(PrimaryTokenKind.AccessToken);

        var result = await resolver.ResolveAsync(
            context,
            CreateResponse(access: HeaderDelivery()));

        result.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("Bearer ")]
    [InlineData("Bearer    ")]
    public async Task Access_token_should_reject_blank_values(string value)
    {
        var context = CreateContext();
        context.Request.Headers.Authorization = value;

        var resolver = CreateResolver(PrimaryTokenKind.AccessToken);

        var result = await resolver.ResolveAsync(
            context,
            CreateResponse(access: HeaderDelivery()));

        result.Should().BeNull();
    }

    [Fact]
    public async Task Unsupported_primary_token_kind_should_return_null()
    {
        var context = CreateContext();
        var resolver = CreateResolver((PrimaryTokenKind)999);

        var result = await resolver.ResolveAsync(
            context,
            CreateResponse());

        result.Should().BeNull();
    }

    private static HttpContext CreateContext()
    {
        var context = TestHttpContext.Create();

        var services = new ServiceCollection();
        services.AddSingleton<IDeviceResolver>(
            new FakeDeviceResolver());

        context.RequestServices = services.BuildServiceProvider();

        return context;
    }

    private static ValidateCredentialResolver CreateResolver(
        PrimaryTokenKind kind)
    {
        return new ValidateCredentialResolver(
            new FixedPrimaryCredentialResolver(kind));
    }

    private static EffectiveGrantDelivery CookieDelivery(
        GrantKind kind,
        string name)
    {
        return new EffectiveGrantDelivery(
            kind: kind,
            tokenFormat: TokenFormat.Opaque,
            mode: GrantDeliveryMode.Cookie,
            cookie: new UAuthCookieOptions
            {
                Name = name
            });
    }

    private static EffectiveGrantDelivery HeaderDelivery(
        string name = "Authorization",
        HeaderTokenFormat format = HeaderTokenFormat.Bearer)
    {
        return new EffectiveGrantDelivery(
            kind: GrantKind.AccessToken,
            tokenFormat: TokenFormat.Jwt,
            mode: GrantDeliveryMode.Header,
            name: name,
            headerFormat: format);
    }

    private static EffectiveAuthResponse CreateResponse(
        EffectiveGrantDelivery? session = null,
        EffectiveGrantDelivery? access = null)
    {
        return new EffectiveAuthResponse(
            session ?? EffectiveGrantDelivery.Disabled(GrantKind.Session),
            access ?? EffectiveGrantDelivery.Disabled(GrantKind.AccessToken),
            EffectiveGrantDelivery.Disabled(GrantKind.RefreshToken),
            EffectiveRedirectResponse.Disabled);
    }

    private sealed class FixedPrimaryCredentialResolver
        : IPrimaryCredentialResolver
    {
        private readonly PrimaryTokenKind _kind;

        public FixedPrimaryCredentialResolver(PrimaryTokenKind kind)
        {
            _kind = kind;
        }

        public PrimaryTokenKind Resolve(HttpContext context) => _kind;
    }

    private sealed class FakeDeviceResolver : IDeviceResolver
    {
        public Task<DeviceInfo> ResolveAsync(HttpContext context)
        {
            return Task.FromResult(TestDevice.DefaultDeviceInfo());
        }
    }
}
