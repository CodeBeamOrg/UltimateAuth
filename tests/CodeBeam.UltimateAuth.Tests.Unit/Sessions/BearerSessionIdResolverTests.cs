using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Http;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public sealed class BearerSessionIdResolverTests
{
    private const string ValidSessionId =
        "bearer-session-0000000000000000000000001";

    [Fact]
    public void Name_ShouldBeBearer()
    {
        var sut = new BearerSessionIdResolver();

        sut.Name.Should().Be("bearer");
    }

    [Fact]
    public void Resolve_WhenAuthorizationHeaderMissing_ShouldReturnNull()
    {
        var sut = new BearerSessionIdResolver();
        var context = new DefaultHttpContext();

        var result = sut.Resolve(context);

        result.Should().BeNull();
    }

    [Fact]
    public void Resolve_WhenAuthorizationSchemeIsNotBearer_ShouldReturnNull()
    {
        var sut = new BearerSessionIdResolver();
        var context = new DefaultHttpContext();

        context.Request.Headers.Authorization =
            $"Basic {ValidSessionId}";

        var result = sut.Resolve(context);

        result.Should().BeNull();
    }

    [Fact]
    public void Resolve_WhenBearerValueIsEmpty_ShouldReturnNull()
    {
        var sut = new BearerSessionIdResolver();
        var context = new DefaultHttpContext();

        context.Request.Headers.Authorization = "Bearer   ";

        var result = sut.Resolve(context);

        result.Should().BeNull();
    }

    [Fact]
    public void Resolve_WhenBearerValueIsInvalid_ShouldReturnNull()
    {
        var sut = new BearerSessionIdResolver();
        var context = new DefaultHttpContext();

        context.Request.Headers.Authorization =
            "Bearer invalid";

        var result = sut.Resolve(context);

        result.Should().BeNull();
    }

    [Fact]
    public void Resolve_WhenBearerValueIsValid_ShouldReturnSessionId()
    {
        var sut = new BearerSessionIdResolver();
        var context = new DefaultHttpContext();

        context.Request.Headers.Authorization =
            $"Bearer {ValidSessionId}";

        var result = sut.Resolve(context);

        result.Should().NotBeNull();
        result!.Value.ToString().Should().Be(ValidSessionId);
    }

    [Fact]
    public void Resolve_WhenBearerSchemeUsesDifferentCasing_ShouldResolveSessionId()
    {
        var sut = new BearerSessionIdResolver();
        var context = new DefaultHttpContext();

        context.Request.Headers.Authorization =
            $"bEaReR {ValidSessionId}";

        var result = sut.Resolve(context);

        result.Should().NotBeNull();
        result!.Value.ToString().Should().Be(ValidSessionId);
    }

    [Fact]
    public void Resolve_WhenBearerValueContainsOuterWhitespace_ShouldTrimAndResolve()
    {
        var sut = new BearerSessionIdResolver();
        var context = new DefaultHttpContext();

        context.Request.Headers.Authorization =
            $"Bearer   {ValidSessionId}   ";

        var result = sut.Resolve(context);

        result.Should().NotBeNull();
        result!.Value.ToString().Should().Be(ValidSessionId);
    }
}
