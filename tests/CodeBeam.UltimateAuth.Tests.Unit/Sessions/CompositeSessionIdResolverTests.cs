using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public sealed class CompositeSessionIdResolverTests
{
    [Fact]
    public void Resolve_ShouldUseFirstSuccessfulResolverAccordingToConfiguredOrder()
    {
        var firstId =
            CreateSessionId(
                "first-session-00000000000000000000000001");

        var secondId =
            CreateSessionId(
                "second-session-0000000000000000000000001");

        var bearer =
            CreateResolver(
                "bearer",
                firstId);

        var cookie =
            CreateResolver(
                "cookie",
                secondId);

        var options = TestServerOptions.Default();

        options.SessionResolution.Order =
            new List<string> { "Bearer", "Cookie" };

        options.SessionResolution.EnableBearer = true;
        options.SessionResolution.EnableCookie = true;

        var sut =
            new CompositeSessionIdResolver(
                new[]
                {
                    bearer.Object,
                    cookie.Object
                },
                Options.Create(options));

        var context = new DefaultHttpContext();

        var result = sut.Resolve(context);

        result.Should().Be(firstId);

        bearer.Verify(
            x => x.Resolve(context),
            Times.Once);

        cookie.Verify(
            x => x.Resolve(context),
            Times.Never);
    }

    [Fact]
    public void Resolve_WhenFirstResolverReturnsNull_ShouldContinueToNextResolver()
    {
        var expected =
            CreateSessionId(
                "cookie-session-0000000000000000000000002");

        var bearer =
            CreateResolver(
                "bearer",
                null);

        var cookie =
            CreateResolver(
                "cookie",
                expected);

        var options = TestServerOptions.Default();

        options.SessionResolution.Order =
            new List<string> { "Bearer", "Cookie" };

        options.SessionResolution.EnableBearer = true;
        options.SessionResolution.EnableCookie = true;

        var sut =
            new CompositeSessionIdResolver(
                new[]
                {
                    bearer.Object,
                    cookie.Object
                },
                Options.Create(options));

        var context = new DefaultHttpContext();

        var result = sut.Resolve(context);

        result.Should().Be(expected);

        bearer.Verify(
            x => x.Resolve(context),
            Times.Once);

        cookie.Verify(
            x => x.Resolve(context),
            Times.Once);
    }

    [Fact]
    public void Resolve_WhenResolverIsDisabled_ShouldSkipIt()
    {
        var bearerId =
            CreateSessionId(
                "bearer-session-0000000000000000000000002");

        var cookieId =
            CreateSessionId(
                "cookie-session-0000000000000000000000003");

        var bearer =
            CreateResolver(
                "bearer",
                bearerId);

        var cookie =
            CreateResolver(
                "cookie",
                cookieId);

        var options = TestServerOptions.Default();

        options.SessionResolution.Order =
            new List<string> { "Bearer", "Cookie" };

        options.SessionResolution.EnableBearer = false;
        options.SessionResolution.EnableCookie = true;

        var sut =
            new CompositeSessionIdResolver(
                new[]
                {
                    bearer.Object,
                    cookie.Object
                },
                Options.Create(options));

        var context = new DefaultHttpContext();

        var result = sut.Resolve(context);

        result.Should().Be(cookieId);

        bearer.Verify(
            x => x.Resolve(
                It.IsAny<HttpContext>()),
            Times.Never);

        cookie.Verify(
            x => x.Resolve(context),
            Times.Once);
    }

    [Fact]
    public void Resolve_WhenConfiguredResolverDoesNotExist_ShouldContinue()
    {
        var expected =
            CreateSessionId(
                "cookie-session-0000000000000000000000004");

        var cookie =
            CreateResolver(
                "cookie",
                expected);

        var options = TestServerOptions.Default();

        options.SessionResolution.Order =
            new List<string> { "Bearer", "Cookie" };

        options.SessionResolution.EnableBearer = true;
        options.SessionResolution.EnableCookie = true;

        var sut =
            new CompositeSessionIdResolver(
                new[]
                {
                    cookie.Object
                },
                Options.Create(options));

        var result =
            sut.Resolve(
                new DefaultHttpContext());

        result.Should().Be(expected);
    }

    [Fact]
    public void Resolve_WhenOrderUsesDifferentCasing_ShouldResolveCaseInsensitively()
    {
        var expected =
            CreateSessionId(
                "bearer-session-0000000000000000000000005");

        var bearer =
            CreateResolver(
                "bearer",
                expected);

        var options = TestServerOptions.Default();

        options.SessionResolution.Order =
            new List<string> { "bEaReR" };

        options.SessionResolution.EnableBearer = true;

        var sut =
            new CompositeSessionIdResolver(
                new[]
                {
                    bearer.Object
                },
                Options.Create(options));

        var result =
            sut.Resolve(
                new DefaultHttpContext());

        result.Should().Be(expected);
    }

    [Fact]
    public void Resolve_WhenNoResolverProducesSessionId_ShouldReturnNull()
    {
        var bearer =
            CreateResolver(
                "bearer",
                null);

        var cookie =
            CreateResolver(
                "cookie",
                null);

        var options = TestServerOptions.Default();

        options.SessionResolution.Order =
            new List<string> { "Bearer", "Cookie" };

        options.SessionResolution.EnableBearer = true;
        options.SessionResolution.EnableCookie = true;

        var sut =
            new CompositeSessionIdResolver(
                new[]
                {
                    bearer.Object,
                    cookie.Object
                },
                Options.Create(options));

        var result =
            sut.Resolve(
                new DefaultHttpContext());

        result.Should().BeNull();
    }

    [Fact]
    public void Resolve_WhenOrderChanges_ShouldRespectNewPrecedence()
    {
        var bearerId =
            CreateSessionId(
                "bearer-session-0000000000000000000000006");

        var cookieId =
            CreateSessionId(
                "cookie-session-0000000000000000000000006");

        var bearer =
            CreateResolver(
                "bearer",
                bearerId);

        var cookie =
            CreateResolver(
                "cookie",
                cookieId);

        var options = TestServerOptions.Default();

        options.SessionResolution.Order =
            new List<string> { "Cookie", "Bearer" };

        options.SessionResolution.EnableBearer = true;
        options.SessionResolution.EnableCookie = true;

        var sut =
            new CompositeSessionIdResolver(
                new[]
                {
                    bearer.Object,
                    cookie.Object
                },
                Options.Create(options));

        var result =
            sut.Resolve(
                new DefaultHttpContext());

        result.Should().Be(cookieId);
    }

    private static Mock<IInnerSessionIdResolver> CreateResolver(
        string name,
        AuthSessionId? result)
    {
        var resolver =
            new Mock<IInnerSessionIdResolver>();

        resolver
            .SetupGet(x => x.Name)
            .Returns(name);

        resolver
            .Setup(x => x.Resolve(
                It.IsAny<HttpContext>()))
            .Returns(result);

        return resolver;
    }

    private static AuthSessionId CreateSessionId(
        string value)
    {
        AuthSessionId.TryCreate(
                value,
                out var id)
            .Should()
            .BeTrue();

        return id;
    }
}
