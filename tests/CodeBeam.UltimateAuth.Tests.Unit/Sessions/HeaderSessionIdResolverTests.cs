using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public sealed class HeaderSessionIdResolverTests
{
    private const string ValidSessionId =
        "header-session-0000000000000000000000001";

    [Fact]
    public void Name_ShouldBeHeader()
    {
        var sut = CreateSut();

        sut.Name.Should().Be("header");
    }

    [Fact]
    public void Resolve_WhenConfiguredHeaderMissing_ShouldReturnNull()
    {
        var sut = CreateSut();
        var context = new DefaultHttpContext();

        var result = sut.Resolve(context);

        result.Should().BeNull();
    }

    [Fact]
    public void Resolve_WhenHeaderIsEmpty_ShouldReturnNull()
    {
        var sut = CreateSut();
        var context = new DefaultHttpContext();

        context.Request.Headers["X-UAuth-Session"] = "";

        var result = sut.Resolve(context);

        result.Should().BeNull();
    }

    [Fact]
    public void Resolve_WhenHeaderContainsInvalidSessionId_ShouldReturnNull()
    {
        var sut = CreateSut();
        var context = new DefaultHttpContext();

        context.Request.Headers["X-UAuth-Session"] = "invalid";

        var result = sut.Resolve(context);

        result.Should().BeNull();
    }

    [Fact]
    public void Resolve_WhenHeaderContainsValidSessionId_ShouldReturnSessionId()
    {
        var sut = CreateSut();
        var context = new DefaultHttpContext();

        context.Request.Headers["X-UAuth-Session"] =
            ValidSessionId;

        var result = sut.Resolve(context);

        result.Should().NotBeNull();
        result!.Value.ToString().Should().Be(ValidSessionId);
    }

    [Fact]
    public void Resolve_ShouldUseConfiguredHeaderName()
    {
        var options = TestServerOptions.Default();

        options.SessionResolution.HeaderName =
            "X-Custom-Session";

        var sut =
            new HeaderSessionIdResolver(
                Options.Create(options));

        var context = new DefaultHttpContext();

        context.Request.Headers["X-UAuth-Session"] =
            "wrong-header-session-000000000000000000001";

        context.Request.Headers["X-Custom-Session"] =
            ValidSessionId;

        var result = sut.Resolve(context);

        result.Should().NotBeNull();
        result!.Value.ToString().Should().Be(ValidSessionId);
    }

    private static HeaderSessionIdResolver CreateSut()
    {
        var options = TestServerOptions.Default();

        options.SessionResolution.HeaderName =
            "X-UAuth-Session";

        return new HeaderSessionIdResolver(
            Options.Create(options));
    }
}
