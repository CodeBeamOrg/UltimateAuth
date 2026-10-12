using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class CredentialInfrastructureTests
{
    [Theory]
    [InlineData("/", false)]
    [InlineData("/login", false)]
    [InlineData("/api", true)]
    [InlineData("/api/users", true)]
    [InlineData("/apiv2", false)]
    public void Primary_resolver_should_classify_request_path(
        string path,
        bool isApi)
    {
        var resolver = CreatePrimaryResolver();
        var context = new DefaultHttpContext();

        context.Request.Path = path;

        var result = resolver.Resolve(context);

        result.Should().Be(
            isApi
                ? PrimaryTokenKind.AccessToken
                : PrimaryTokenKind.Session);
    }

    [Fact]
    public void Authorization_header_should_select_api_credential()
    {
        var resolver = CreatePrimaryResolver();
        var context = new DefaultHttpContext();

        context.Request.Path = "/dashboard";
        context.Request.Headers.Authorization = "Bearer token";

        resolver.Resolve(context)
            .Should().Be(PrimaryTokenKind.AccessToken);
    }

    [Fact]
    public void Primary_resolver_should_use_configured_credential_kinds()
    {
        var options = new UAuthServerOptions();

        options.PrimaryCredential.Ui = PrimaryTokenKind.AccessToken;
        options.PrimaryCredential.Api = PrimaryTokenKind.Session;

        var resolver = new PrimaryCredentialResolver(
            Options.Create(options));

        var ui = new DefaultHttpContext();
        ui.Request.Path = "/dashboard";

        var api = new DefaultHttpContext();
        api.Request.Path = "/api/users";

        resolver.Resolve(ui).Should().Be(PrimaryTokenKind.AccessToken);
        resolver.Resolve(api).Should().Be(PrimaryTokenKind.Session);
    }

    [Theory]
    [InlineData("token", "Bearer token")]
    [InlineData("abc123", "Bearer abc123")]
    public void Header_builder_should_format_bearer_tokens(
        string raw,
        string expected)
    {
        var builder = new UAuthHeaderPolicyBuilder();

        var options = new CredentialResponseOptions
        {
            HeaderFormat = HeaderTokenFormat.Bearer
        };

        builder.BuildHeaderValue(raw, options, null!)
            .Should().Be(expected);
    }

    [Fact]
    public void Header_builder_should_preserve_raw_tokens()
    {
        var builder = new UAuthHeaderPolicyBuilder();

        var options = new CredentialResponseOptions
        {
            HeaderFormat = HeaderTokenFormat.Raw
        };

        builder.BuildHeaderValue("raw-token", options, null!)
            .Should().Be("raw-token");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Header_builder_should_reject_empty_values(string? value)
    {
        var builder = new UAuthHeaderPolicyBuilder();

        var options = new CredentialResponseOptions
        {
            HeaderFormat = HeaderTokenFormat.Bearer
        };

        Action act = () =>
            builder.BuildHeaderValue(value!, options, null!);

        act.Should()
            .Throw<ArgumentException>()
            .WithParameterName("rawValue");
    }

    [Fact]
    public void Header_builder_should_reject_unsupported_format()
    {
        var builder = new UAuthHeaderPolicyBuilder();

        var options = new CredentialResponseOptions
        {
            HeaderFormat = (HeaderTokenFormat)999
        };

        Action act = () =>
            builder.BuildHeaderValue("token", options, null!);

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage("*Unsupported header token format*");
    }

    private static PrimaryCredentialResolver CreatePrimaryResolver()
    {
        return new PrimaryCredentialResolver(
            Options.Create(new UAuthServerOptions()));
    }
}
