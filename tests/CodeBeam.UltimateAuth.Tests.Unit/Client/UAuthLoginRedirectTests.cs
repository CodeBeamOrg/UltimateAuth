using Bunit;
using CodeBeam.UltimateAuth.Client.Blazor;
using CodeBeam.UltimateAuth.Core.Defaults;
using FluentAssertions;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Tests.Unit.Client.Blazor;

public sealed class UAuthLoginRedirectTests : BunitContext
{
    private NavigationManager Nav =>
        Services.GetRequiredService<NavigationManager>();

    [Fact]
    public void Render_WithoutReturnUrl_NavigatesToLoginPage()
    {
        Navigate(UAuthConstants.Routes.LoginRedirect);

        Render<UAuthLoginDispatch>();

        Nav.Uri.Should().Be("http://localhost/login");
    }

    [Fact]
    public void Render_WithRelativeReturnUrl_PreservesReturnUrl()
    {
        NavigateToRedirect("/home");

        Render<UAuthLoginDispatch>();

        Nav.Uri.Should().Be(
            "http://localhost/login?uauth_return_url=%2Fhome");
    }

    [Fact]
    public void Render_WithNestedRelativeReturnUrl_PreservesAndEncodesReturnUrl()
    {
        NavigateToRedirect("/account/security?tab=sessions");

        Render<UAuthLoginDispatch>();

        var uri = Nav.ToAbsoluteUri(Nav.Uri);

        uri.AbsolutePath.Should().Be("/login");

        var query =
            Microsoft.AspNetCore.WebUtilities.QueryHelpers
                .ParseQuery(uri.Query);

        query[UAuthConstants.Query.ReturnUrl]
            .ToString()
            .Should()
            .Be("/account/security?tab=sessions");
    }

    [Fact]
    public void Render_WithDotRelativeReturnUrl_PreservesReturnUrl()
    {
        NavigateToRedirect("./home");

        Render<UAuthLoginDispatch>();

        GetReturnUrlFromCurrentUri()
            .Should()
            .Be("./home");
    }

    [Fact]
    public void Render_WithParentRelativeReturnUrl_PreservesReturnUrl()
    {
        NavigateToRedirect("../home");

        Render<UAuthLoginDispatch>();

        GetReturnUrlFromCurrentUri()
            .Should()
            .Be("../home");
    }

    [Fact]
    public void Render_WithAbsoluteHttpsReturnUrl_PreservesReturnUrl()
    {
        const string returnUrl =
            "https://example.com/account/security";

        NavigateToRedirect(returnUrl);

        Render<UAuthLoginDispatch>();

        GetReturnUrlFromCurrentUri()
            .Should()
            .Be(returnUrl);
    }

    [Fact]
    public void Render_WithAbsoluteHttpReturnUrl_PreservesReturnUrl()
    {
        const string returnUrl =
            "http://example.com/account/security";

        NavigateToRedirect(returnUrl);

        Render<UAuthLoginDispatch>();

        GetReturnUrlFromCurrentUri()
            .Should()
            .Be(returnUrl);
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://example.com/file")]
    [InlineData("mailto:test@example.com")]
    public void Render_WithUnsupportedAbsoluteScheme_DropsReturnUrl(
        string returnUrl)
    {
        NavigateToRedirect(returnUrl);

        Render<UAuthLoginDispatch>();

        Nav.ToAbsoluteUri(Nav.Uri)
            .AbsolutePath
            .Should()
            .Be("/login");

        GetReturnUrlFromCurrentUri()
            .Should()
            .BeNull();
    }

    [Fact]
    public void Render_WithFreshLogin_DropsReturnUrl()
    {
        var uri = Nav.GetUriWithQueryParameters(
            UAuthConstants.Routes.LoginRedirect,
            new Dictionary<string, object?>
            {
                ["fresh"] = "1",
                [UAuthConstants.Query.ReturnUrl] = "/home"
            });

        Nav.NavigateTo(uri);

        Render<UAuthLoginDispatch>();

        Nav.ToAbsoluteUri(Nav.Uri)
            .AbsolutePath
            .Should()
            .Be("/login");

        GetReturnUrlFromCurrentUri()
            .Should()
            .BeNull();
    }

    [Fact]
    public void Render_WithFreshParameterRegardlessOfValue_DropsReturnUrl()
    {
        var uri = Nav.GetUriWithQueryParameters(
            UAuthConstants.Routes.LoginRedirect,
            new Dictionary<string, object?>
            {
                ["fresh"] = "false",
                [UAuthConstants.Query.ReturnUrl] = "/home"
            });

        Nav.NavigateTo(uri);

        Render<UAuthLoginDispatch>();

        GetReturnUrlFromCurrentUri()
            .Should()
            .BeNull();
    }

    [Fact]
    public void Render_WithLegacyReturnUrlQuery_DoesNotConsumeIt()
    {
        Navigate(
            $"{UAuthConstants.Routes.LoginRedirect}" +
            "?return_url=%2Fhome");

        Render<UAuthLoginDispatch>();

        Nav.ToAbsoluteUri(Nav.Uri)
            .AbsolutePath
            .Should()
            .Be("/login");

        GetReturnUrlFromCurrentUri()
            .Should()
            .BeNull();
    }

    [Fact]
    public void Render_WithUAuthReturnUrl_ConsumesIt()
    {
        NavigateToRedirect("/home");

        Render<UAuthLoginDispatch>();

        GetReturnUrlFromCurrentUri()
            .Should()
            .Be("/home");
    }

    private void NavigateToRedirect(string returnUrl)
    {
        Nav.NavigateTo(UAuthConstants.Routes.LoginRedirect);

        var uri = Nav.GetUriWithQueryParameter(UAuthConstants.Query.ReturnUrl, returnUrl);

        Nav.NavigateTo(uri);
    }

    private void Navigate(string relativeUri)
    {
        Nav.NavigateTo(relativeUri);
    }

    private string? GetReturnUrlFromCurrentUri()
    {
        var uri = Nav.ToAbsoluteUri(Nav.Uri);

        var query =
            Microsoft.AspNetCore.WebUtilities.QueryHelpers
                .ParseQuery(uri.Query);

        return query.TryGetValue(
            UAuthConstants.Query.ReturnUrl,
            out var value)
                ? value.ToString()
                : null;
    }
}