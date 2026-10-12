using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class CredentialResponseWriterTests
{
    [Theory]
    [InlineData(GrantKind.Session)]
    [InlineData(GrantKind.AccessToken)]
    [InlineData(GrantKind.RefreshToken)]
    public void None_delivery_should_not_write_credentials(GrantKind kind)
    {
        var setup = CreateWriter(CreateResponse());
        var context = TestHttpContext.Create();

        setup.Writer.WriteInternal(context, kind, "secret-token");

        context.Response.Headers.Should().BeEmpty();
        setup.CookieManager.WriteCount.Should().Be(0);
        setup.CookiePolicy.BuildCount.Should().Be(0);
    }

    [Theory]
    [InlineData(GrantKind.Session)]
    [InlineData(GrantKind.AccessToken)]
    [InlineData(GrantKind.RefreshToken)]
    public void Header_delivery_should_write_correct_grant(GrantKind kind)
    {
        var delivery = new CredentialResponseOptions
        {
            Mode = TokenResponseMode.Header,
            HeaderFormat = HeaderTokenFormat.Bearer
        };

        var setup = CreateWriter(CreateResponse(kind, delivery));
        var context = TestHttpContext.Create();

        setup.Writer.WriteInternal(context, kind, "secret-token");

        context.Response.Headers.Authorization.ToString()
            .Should().Be("Bearer secret-token");

        setup.CookieManager.WriteCount.Should().Be(0);
    }

    [Fact]
    public void Raw_header_delivery_should_not_add_bearer_prefix()
    {
        var delivery = new CredentialResponseOptions
        {
            Mode = TokenResponseMode.Header,
            HeaderFormat = HeaderTokenFormat.Raw
        };

        var setup = CreateWriter(
            CreateResponse(GrantKind.AccessToken, delivery));

        var context = TestHttpContext.Create();

        setup.Writer.WriteInternal(
            context,
            GrantKind.AccessToken,
            "raw-token");

        context.Response.Headers.Authorization.ToString()
            .Should().Be("raw-token");
    }

    [Fact]
    public void Header_delivery_should_support_custom_header_name()
    {
        var delivery = new CredentialResponseOptions
        {
            Mode = TokenResponseMode.Header,
            Name = "X-UAuth-Token",
            HeaderFormat = HeaderTokenFormat.Raw
        };

        var setup = CreateWriter(
            CreateResponse(GrantKind.AccessToken, delivery));

        var context = TestHttpContext.Create();

        setup.Writer.WriteInternal(
            context,
            GrantKind.AccessToken,
            "custom-token");

        context.Response.Headers["X-UAuth-Token"].ToString()
            .Should().Be("custom-token");

        context.Response.Headers.ContainsKey("Authorization")
            .Should().BeFalse();
    }

    [Fact]
    public void Header_delivery_should_replace_existing_header()
    {
        var delivery = new CredentialResponseOptions
        {
            Mode = TokenResponseMode.Header,
            HeaderFormat = HeaderTokenFormat.Bearer
        };

        var setup = CreateWriter(
            CreateResponse(GrantKind.AccessToken, delivery));

        var context = TestHttpContext.Create();
        context.Response.Headers.Authorization = "Bearer old-token";

        setup.Writer.WriteInternal(
            context,
            GrantKind.AccessToken,
            "new-token");

        context.Response.Headers.Authorization.ToString()
            .Should().Be("Bearer new-token");
    }

    [Theory]
    [InlineData(GrantKind.Session)]
    [InlineData(GrantKind.AccessToken)]
    [InlineData(GrantKind.RefreshToken)]
    public void Cookie_delivery_should_write_correct_credential(
        GrantKind kind)
    {
        var delivery = new CredentialResponseOptions
        {
            Mode = TokenResponseMode.Cookie
        }.WithCookie(new UAuthCookieOptions
        {
            Name = "uauth-credential"
        });

        var setup = CreateWriter(CreateResponse(kind, delivery));
        var context = TestHttpContext.Create();

        setup.Writer.WriteInternal(context, kind, "cookie-token");

        setup.CookieManager.WriteCount.Should().Be(1);
        setup.CookieManager.LastName.Should().Be("uauth-credential");
        setup.CookieManager.LastValue.Should().Be("cookie-token");
        setup.CookieManager.LastContext.Should().BeSameAs(context);

        setup.CookiePolicy.BuildCount.Should().Be(1);
        setup.CookiePolicy.LastKind.Should().Be(kind);
        setup.CookiePolicy.LastDelivery.Should().BeSameAs(delivery);
        setup.CookiePolicy.LastAuth.Should().BeSameAs(setup.Auth);

        setup.CookieManager.LastOptions
            .Should().BeSameAs(setup.CookiePolicy.CookieOptions);
    }

    [Theory]
    [InlineData(GrantKind.Session)]
    [InlineData(GrantKind.AccessToken)]
    [InlineData(GrantKind.RefreshToken)]
    public void Cookie_delivery_without_options_should_throw(
        GrantKind kind)
    {
        var delivery = new CredentialResponseOptions
        {
            Mode = TokenResponseMode.Cookie
        };

        var setup = CreateWriter(CreateResponse(kind, delivery));
        var context = TestHttpContext.Create();

        Action act = () =>
            setup.Writer.WriteInternal(context, kind, "token");

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*Cookie options missing*");

        setup.CookieManager.WriteCount.Should().Be(0);
        setup.CookiePolicy.BuildCount.Should().Be(0);
    }

    [Theory]
    [InlineData(GrantKind.Session)]
    [InlineData(GrantKind.AccessToken)]
    [InlineData(GrantKind.RefreshToken)]
    public void Body_delivery_should_throw_not_supported(
        GrantKind kind)
    {
        var delivery = new CredentialResponseOptions
        {
            Mode = TokenResponseMode.Body
        };

        var setup = CreateWriter(CreateResponse(kind, delivery));
        var context = TestHttpContext.Create();

        Action act = () =>
            setup.Writer.WriteInternal(context, kind, "token");

        act.Should()
            .Throw<NotSupportedException>()
            .WithMessage("*Body token delivery is not supported*");
    }

    [Fact]
    public void Unsupported_grant_kind_should_throw()
    {
        var setup = CreateWriter(CreateResponse());
        var context = TestHttpContext.Create();

        Action act = () =>
            setup.Writer.WriteInternal(
                context,
                (GrantKind)999,
                "token");

        act.Should()
            .Throw<ArgumentOutOfRangeException>()
            .WithParameterName("kind");
    }

    [Fact]
    public void Header_policy_failure_should_propagate()
    {
        var delivery = new CredentialResponseOptions
        {
            Mode = TokenResponseMode.Header,
            HeaderFormat = (HeaderTokenFormat)999
        };

        var setup = CreateWriter(
            CreateResponse(GrantKind.AccessToken, delivery));

        var context = TestHttpContext.Create();

        Action act = () =>
            setup.Writer.WriteInternal(
                context,
                GrantKind.AccessToken,
                "token");

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*Unsupported header token format*");

        context.Response.Headers.ContainsKey("Authorization")
            .Should().BeFalse();
    }

    private static WriterSetup CreateWriter(EffectiveAuthResponse response)
    {
        var auth = AuthFlowTestFactory.New(response: response);
        var accessor = new FakeAuthFlowContextAccessor(auth);
        var cookieManager = new FakeCookieManager();
        var cookiePolicy = new FakeCookiePolicyBuilder();

        var writer = new CredentialResponseWriter(
            accessor,
            cookieManager,
            cookiePolicy,
            new UAuthHeaderPolicyBuilder());

        return new WriterSetup(
            writer,
            auth,
            cookieManager,
            cookiePolicy);
    }

    private static EffectiveAuthResponse CreateResponse(
        GrantKind? kind = null,
        CredentialResponseOptions? delivery = null)
    {
        var session = CredentialResponseOptions.Disabled(GrantKind.Session);
        var access = CredentialResponseOptions.Disabled(GrantKind.AccessToken);
        var refresh = CredentialResponseOptions.Disabled(GrantKind.RefreshToken);

        switch (kind)
        {
            case GrantKind.Session:
                session = delivery!;
                break;

            case GrantKind.AccessToken:
                access = delivery!;
                break;

            case GrantKind.RefreshToken:
                refresh = delivery!;
                break;
        }

        return new EffectiveAuthResponse(
            session,
            access,
            refresh,
            EffectiveRedirectResponse.Disabled);
    }

    private sealed record WriterSetup(
        CredentialResponseWriter Writer,
        AuthFlowContext Auth,
        FakeCookieManager CookieManager,
        FakeCookiePolicyBuilder CookiePolicy);

    private sealed class FakeAuthFlowContextAccessor
        : IAuthFlowContextAccessor
    {
        public FakeAuthFlowContextAccessor(AuthFlowContext current)
        {
            Current = current;
        }

        public AuthFlowContext Current { get; }
    }

    private sealed class FakeCookieManager : IUAuthCookieManager
    {
        public int WriteCount { get; private set; }
        public HttpContext? LastContext { get; private set; }
        public string? LastName { get; private set; }
        public string? LastValue { get; private set; }
        public CookieOptions? LastOptions { get; private set; }

        public void Write(
            HttpContext context,
            string name,
            string value,
            CookieOptions options)
        {
            WriteCount++;
            LastContext = context;
            LastName = name;
            LastValue = value;
            LastOptions = options;
        }

        public bool TryRead(
            HttpContext context,
            string name,
            out string value)
        {
            value = string.Empty;
            return false;
        }

        public void Delete(HttpContext context, string name)
        {
        }
    }

    private sealed class FakeCookiePolicyBuilder
        : IUAuthCookiePolicyBuilder
    {
        public int BuildCount { get; private set; }
        public GrantKind? LastKind { get; private set; }
        public CredentialResponseOptions? LastDelivery { get; private set; }
        public AuthFlowContext? LastAuth { get; private set; }

        public CookieOptions CookieOptions { get; } = new()
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict
        };

        public CookieOptions Build(
            CredentialResponseOptions response,
            AuthFlowContext context,
            GrantKind kind)
        {
            BuildCount++;
            LastDelivery = response;
            LastAuth = context;
            LastKind = kind;

            return CookieOptions;
        }
    }
}
