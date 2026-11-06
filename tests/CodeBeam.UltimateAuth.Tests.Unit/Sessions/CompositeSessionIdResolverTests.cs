using FluentAssertions;
using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using Microsoft.AspNetCore.Http;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public sealed class CompositeSessionIdResolverTests
{
    private const UAuthClientProfile Profile = UAuthClientProfile.BlazorServer;

    [Fact]
    public async Task ResolveAsync_ShouldUseFirstSuccessfulResolverAccordingToConfiguredOrder()
    {
        var firstId = CreateSessionId("first-session-00000000000000000000000001");
        var secondId = CreateSessionId("second-session-0000000000000000000000001");

        var bearer = CreateResolver("Bearer", firstId);
        var cookie = CreateResolver("Cookie", secondId);

        var options = CreateOptions("Bearer", "Cookie");

        var (sut, context) = CreateSut(
            options,
            bearer.Object,
            cookie.Object);

        var result = await sut.ResolveAsync(context);

        result.Should().Be(firstId);
        bearer.Verify(x => x.Resolve(context), Times.Once);
        cookie.Verify(x => x.Resolve(context), Times.Never);
    }

    [Fact]
    public async Task ResolveAsync_WhenFirstResolverReturnsNull_ShouldContinueToNextResolver()
    {
        var expected = CreateSessionId("cookie-session-0000000000000000000000002");

        var bearer = CreateResolver("Bearer", null);
        var cookie = CreateResolver("Cookie", expected);

        var options = CreateOptions("Bearer", "Cookie");

        var (sut, context) = CreateSut(
            options,
            bearer.Object,
            cookie.Object);

        var result = await sut.ResolveAsync(context);

        result.Should().Be(expected);
        bearer.Verify(x => x.Resolve(context), Times.Once);
        cookie.Verify(x => x.Resolve(context), Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_WhenResolverIsDisabled_ShouldSkipIt()
    {
        var bearerId = CreateSessionId("bearer-session-0000000000000000000000002");
        var cookieId = CreateSessionId("cookie-session-0000000000000000000000003");

        var bearer = CreateResolver("Bearer", bearerId);
        var cookie = CreateResolver("Cookie", cookieId);

        var options = CreateOptions("Bearer", "Cookie");
        options.SessionResolution.EnableBearer = false;

        var (sut, context) = CreateSut(
            options,
            bearer.Object,
            cookie.Object);

        var result = await sut.ResolveAsync(context);

        result.Should().Be(cookieId);
        bearer.Verify(x => x.Resolve(It.IsAny<HttpContext>()), Times.Never);
        cookie.Verify(x => x.Resolve(context), Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_WhenConfiguredResolverDoesNotExist_ShouldContinue()
    {
        var expected = CreateSessionId("cookie-session-0000000000000000000000004");

        var cookie = CreateResolver("Cookie", expected);
        var options = CreateOptions("Bearer", "Cookie");

        var (sut, context) = CreateSut(options, cookie.Object);

        var result = await sut.ResolveAsync(context);

        result.Should().Be(expected);
        cookie.Verify(x => x.Resolve(context), Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_WhenOrderUsesDifferentCasing_ShouldResolveCaseInsensitively()
    {
        var expected = CreateSessionId("bearer-session-0000000000000000000000005");

        var bearer = CreateResolver("bearer", expected);
        var options = CreateOptions("bEaReR");

        var (sut, context) = CreateSut(options, bearer.Object);

        var result = await sut.ResolveAsync(context);

        result.Should().Be(expected);
        bearer.Verify(x => x.Resolve(context), Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_WhenNoResolverProducesSessionId_ShouldReturnNull()
    {
        var bearer = CreateResolver("Bearer", null);
        var cookie = CreateResolver("Cookie", null);

        var options = CreateOptions("Bearer", "Cookie");

        var (sut, context) = CreateSut(
            options,
            bearer.Object,
            cookie.Object);

        var result = await sut.ResolveAsync(context);

        result.Should().BeNull();
        bearer.Verify(x => x.Resolve(context), Times.Once);
        cookie.Verify(x => x.Resolve(context), Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_WhenOrderChanges_ShouldRespectNewPrecedence()
    {
        var bearerId = CreateSessionId("bearer-session-0000000000000000000000006");
        var cookieId = CreateSessionId("cookie-session-0000000000000000000000006");

        var bearer = CreateResolver("Bearer", bearerId);
        var cookie = CreateResolver("Cookie", cookieId);

        var options = CreateOptions("Cookie", "Bearer");

        var (sut, context) = CreateSut(
            options,
            bearer.Object,
            cookie.Object);

        var result = await sut.ResolveAsync(context);

        result.Should().Be(cookieId);
        cookie.Verify(x => x.Resolve(context), Times.Once);
        bearer.Verify(x => x.Resolve(context), Times.Never);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ResolveAsync_ShouldRespectEffectiveCookieSetting(
        bool effectiveCookieEnabled)
    {
        var expected = CreateSessionId("cookie-session-000000000000000000000001");

        var cookie = CreateResolver("Cookie", expected);

        var options = CreateOptions("Cookie");
        options.SessionResolution.EnableCookie = effectiveCookieEnabled;

        var (sut, context) = CreateSut(options, cookie.Object);

        var result = await sut.ResolveAsync(context);

        if (effectiveCookieEnabled)
        {
            result.Should().Be(expected);
            cookie.Verify(x => x.Resolve(context), Times.Once);
        }
        else
        {
            result.Should().BeNull();
            cookie.Verify(x => x.Resolve(context), Times.Never);
        }
    }

    [Fact]
    public async Task ResolveAsync_ShouldReadClientProfileAndUseItsEffectiveOptions()
    {
        var expected = CreateSessionId("cookie-session-000000000000000000000002");

        var cookie = CreateResolver("Cookie", expected);

        var options = CreateOptions("Cookie");

        var profileReader = new Mock<IClientProfileReader>();
        var effectiveProvider = new Mock<IEffectiveServerOptionsProvider>();

        var context = new DefaultHttpContext();

        profileReader
            .Setup(x => x.ReadAsync(context))
            .ReturnsAsync(Profile);

        effectiveProvider
            .Setup(x => x.GetEffective(context, Profile))
            .Returns(new EffectiveUAuthServerOptions
            {
                Mode = UAuthMode.PureOpaque,
                Options = options
            });

        var sut = new CompositeSessionIdResolver(
            new[] { cookie.Object },
            profileReader.Object,
            effectiveProvider.Object);

        var result = await sut.ResolveAsync(context);

        result.Should().Be(expected);

        profileReader.Verify(
            x => x.ReadAsync(context),
            Times.Once);

        effectiveProvider.Verify(
            x => x.GetEffective(context, Profile),
            Times.Once);

        cookie.Verify(
            x => x.Resolve(context),
            Times.Once);
    }

    private static UAuthServerOptions CreateOptions(params string[] order)
    {
        var options = TestServerOptions.Default();

        options.SessionResolution.Order = order.ToList();
        options.SessionResolution.EnableBearer = true;
        options.SessionResolution.EnableHeader = true;
        options.SessionResolution.EnableCookie = true;
        options.SessionResolution.EnableQuery = true;

        return options;
    }

    private static (CompositeSessionIdResolver Sut, DefaultHttpContext Context)
        CreateSut(
            UAuthServerOptions effectiveOptions,
            params IInnerSessionIdResolver[] resolvers)
    {
        var context = new DefaultHttpContext();

        var profileReader = new Mock<IClientProfileReader>();
        profileReader
            .Setup(x => x.ReadAsync(context))
            .ReturnsAsync(Profile);

        var effectiveProvider = new Mock<IEffectiveServerOptionsProvider>();
        effectiveProvider
            .Setup(x => x.GetEffective(context, Profile))
            .Returns(new EffectiveUAuthServerOptions
            {
                Mode = UAuthMode.PureOpaque,
                Options = effectiveOptions
            });

        var sut = new CompositeSessionIdResolver(
            resolvers,
            profileReader.Object,
            effectiveProvider.Object);

        return (sut, context);
    }

    private static Mock<IInnerSessionIdResolver> CreateResolver(string name, AuthSessionId? result)
    {
        var resolver = new Mock<IInnerSessionIdResolver>();

        resolver.SetupGet(x => x.Name).Returns(name);

        resolver.Setup(x => x.Resolve(It.IsAny<HttpContext>())).Returns(result);

        return resolver;
    }

    private static AuthSessionId CreateSessionId(string value)
    {
        AuthSessionId.TryCreate(value, out var id).Should().BeTrue();
        return id;
    }
}
