using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public sealed class QuerySessionIdResolverTests
{
    private const string ValidSessionId =
        "query-session-00000000000000000000000001";

    [Fact]
    public void Name_ShouldBeQuery()
    {
        var sut = CreateSut();

        sut.Name.Should().Be("query");
    }

    [Fact]
    public void Resolve_WhenQueryParameterMissing_ShouldReturnNull()
    {
        var sut = CreateSut();
        var context = new DefaultHttpContext();

        var result = sut.Resolve(context);

        result.Should().BeNull();
    }

    [Fact]
    public void Resolve_WhenQueryParameterIsEmpty_ShouldReturnNull()
    {
        var sut = CreateSut();

        var context = CreateContext(
            "?uauth_session=");

        var result = sut.Resolve(context);

        result.Should().BeNull();
    }

    [Fact]
    public void Resolve_WhenQueryParameterIsInvalid_ShouldReturnNull()
    {
        var sut = CreateSut();

        var context = CreateContext(
            "?uauth_session=invalid");

        var result = sut.Resolve(context);

        result.Should().BeNull();
    }

    [Fact]
    public void Resolve_WhenQueryParameterIsValid_ShouldReturnSessionId()
    {
        var sut = CreateSut();

        var context = CreateContext(
            $"?uauth_session={ValidSessionId}");

        var result = sut.Resolve(context);

        result.Should().NotBeNull();
        result!.Value.ToString().Should().Be(ValidSessionId);
    }

    [Fact]
    public void Resolve_ShouldUseConfiguredQueryParameterName()
    {
        var options = TestServerOptions.Default();

        options.SessionResolution.QueryParameterName =
            "custom_session";

        var sut =
            new QuerySessionIdResolver(
                Options.Create(options));

        var context = CreateContext(
            $"?uauth_session=ignored-session-00000000000000000001&custom_session={ValidSessionId}");

        var result = sut.Resolve(context);

        result.Should().NotBeNull();
        result!.Value.ToString().Should().Be(ValidSessionId);
    }

    private static QuerySessionIdResolver CreateSut()
    {
        var options = TestServerOptions.Default();

        options.SessionResolution.QueryParameterName =
            "uauth_session";

        return new QuerySessionIdResolver(
            Options.Create(options));
    }

    private static DefaultHttpContext CreateContext(
        string queryString)
    {
        var context = new DefaultHttpContext();

        context.Request.QueryString =
            new QueryString(queryString);

        return context;
    }
}
