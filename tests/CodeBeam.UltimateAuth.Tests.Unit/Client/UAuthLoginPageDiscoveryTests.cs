using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Blazor.Infrastructure;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using FluentAssertions;

namespace CodeBeam.UltimateAuth.Tests.Unit.Client.Infrastructure;

public sealed class UAuthLoginPageDiscoveryTests
{
    [Fact]
    public void ResolveRoute_WithNoRoutes_ShouldReturnDefaultLoginRoute()
    {
        var result = UAuthLoginPageDiscovery.ResolveRoute(
            new UAuthLoginPageAttribute(),
            Array.Empty<string>());

        result.Should().Be("/login");
    }

    [Fact]
    public void ResolveRoute_WithSingleRoute_ShouldReturnRoute()
    {
        var result = UAuthLoginPageDiscovery.ResolveRoute(
            new UAuthLoginPageAttribute(),
            new[]
            {
                "/sign-in"
            });

        result.Should().Be("/sign-in");
    }

    [Fact]
    public void ResolveRoute_WithRootRoute_ShouldPreferRoot()
    {
        var result = UAuthLoginPageDiscovery.ResolveRoute(
            new UAuthLoginPageAttribute(),
            new[]
            {
                "/other",
                "/",
                "/login"
            });

        result.Should().Be("/");
    }

    [Fact]
    public void ResolveRoute_WithoutRoot_ShouldPreferLoginRoute()
    {
        var result = UAuthLoginPageDiscovery.ResolveRoute(
            new UAuthLoginPageAttribute(),
            new[]
            {
                "/account",
                "/login",
                "/signin"
            });

        result.Should().Be("/login");
    }

    [Fact]
    public void ResolveRoute_WithPreferredRoute_ShouldPreferExplicitRoute()
    {
        var result = UAuthLoginPageDiscovery.ResolveRoute(
            new UAuthLoginPageAttribute("/sign-in"),
            new[]
            {
                "/",
                "/login",
                "/sign-in"
            });

        result.Should().Be("/sign-in");
    }

    [Fact]
    public void ResolveRoute_WithPreferredRouteWithoutLeadingSlash_ShouldResolveRoute()
    {
        var result = UAuthLoginPageDiscovery.ResolveRoute(
            new UAuthLoginPageAttribute("sign-in"),
            new[]
            {
                "/",
                "/sign-in"
            });

        result.Should().Be("/sign-in");
    }

    [Fact]
    public void ResolveRoute_WithPreferredRoute_ShouldMatchCaseInsensitively()
    {
        var result = UAuthLoginPageDiscovery.ResolveRoute(
            new UAuthLoginPageAttribute("/LOGIN"),
            new[]
            {
                "/",
                "/login"
            });

        result.Should().Be("/login");
    }

    [Fact]
    public void ResolveRoute_WithPreferredRouteTrailingSlash_ShouldNormalizeRoute()
    {
        var result = UAuthLoginPageDiscovery.ResolveRoute(
            new UAuthLoginPageAttribute("/login/"),
            new[]
            {
                "/login"
            });

        result.Should().Be("/login");
    }

    [Fact]
    public void ResolveRoute_WithUndefinedPreferredRoute_ShouldThrow()
    {
        var act = () =>
            UAuthLoginPageDiscovery.ResolveRoute(
                new UAuthLoginPageAttribute("/sign-in"),
                new[]
                {
                    "/",
                    "/login"
                },
                "TestLoginPage");

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage(
                "*Preferred login route '/sign-in'*TestLoginPage*");
    }

    [Fact]
    public void ResolveRoute_WithMultipleCustomRoutes_ShouldUseDeterministicFallback()
    {
        var result = UAuthLoginPageDiscovery.ResolveRoute(
            new UAuthLoginPageAttribute(),
            new[]
            {
                "/z-login",
                "/custom-login",
                "/account"
            });

        result.Should().Be("/account");
    }

    [Fact]
    public void ResolveRoute_ShouldNormalizeRoutes()
    {
        var result = UAuthLoginPageDiscovery.ResolveRoute(
            new UAuthLoginPageAttribute(),
            new[]
            {
                "login/"
            });

        result.Should().Be("/login");
    }

    [Fact]
    public void ResolveRoute_ShouldIgnoreDuplicateRoutes()
    {
        var result = UAuthLoginPageDiscovery.ResolveRoute(
            new UAuthLoginPageAttribute(),
            new[]
            {
                "/login",
                "/LOGIN",
                "/login/"
            });

        result.Should().Be("/login");
    }

    [Theory]
    [InlineData("/", "/login")]
    [InlineData("/login", "/")]
    public void ResolveRoute_WithRootAndLogin_ShouldAlwaysPreferRoot_RegardlessOfDiscoveryOrder(
        string first,
        string second)
    {
        var result = UAuthLoginPageDiscovery.ResolveRoute(
            new UAuthLoginPageAttribute(),
            new[]
            {
                first,
                second
            });

        result.Should().Be("/");
    }

    [Theory]
    [InlineData("/z", "/a", "/m")]
    [InlineData("/m", "/z", "/a")]
    [InlineData("/a", "/m", "/z")]
    public void ResolveRoute_CustomFallback_ShouldBeIndependentOfDiscoveryOrder(
        string first,
        string second,
        string third)
    {
        var result = UAuthLoginPageDiscovery.ResolveRoute(
            new UAuthLoginPageAttribute(),
            new[]
            {
                first,
                second,
                third
            });

        result.Should().Be("/a");
    }
}