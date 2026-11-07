
using AwesomeAssertions;
using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Tests.Integration.Infrastructure;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Tests.Integration.Security;

public sealed class AntiforgeryCookiePolicyTests
{
    [Theory]
    [InlineData(UAuthHubDeploymentMode.Embedded, SameSiteMode.Strict)]
    [InlineData(UAuthHubDeploymentMode.Integrated, SameSiteMode.Strict)]
    [InlineData(UAuthHubDeploymentMode.External, SameSiteMode.None)]
    public async Task AntiforgeryCookie_UsesDeploymentPolicy(
        UAuthHubDeploymentMode deployment,
        SameSiteMode expected)
    {
        await using var factory = AuthServerFactory.Create(
            options => options.HubDeploymentMode = deployment);

        var antiforgery = factory.Services
            .GetRequiredService<IOptions<AntiforgeryOptions>>()
            .Value;

        antiforgery.Cookie.SameSite.Should().Be(expected);

        if (expected == SameSiteMode.None)
        {
            antiforgery.Cookie.SecurePolicy
                .Should().Be(CookieSecurePolicy.Always);
        }
    }

    [Fact]
    public async Task ExternalHub_AntiforgeryTokenCookie_IsSecureAndCrossSite()
    {
        await using var factory = AuthServerFactory.Create(
            options => options.HubDeploymentMode =
                UAuthHubDeploymentMode.External);

        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost"),
                AllowAutoRedirect = false
            });

        using var response = await client.GetAsync(
            "/auth/antiforgery/token",
            TestContext.Current.CancellationToken);

        response.EnsureSuccessStatusCode();

        response.Headers.TryGetValues("Set-Cookie", out var cookies)
            .Should().BeTrue();

        var cookie = cookies!.Single(x =>
            x.StartsWith(
                ".AspNetCore.Antiforgery.",
                StringComparison.OrdinalIgnoreCase));

        cookie.Contains(
            "samesite=none",
            StringComparison.OrdinalIgnoreCase).Should().BeTrue();

        cookie.Split(';', StringSplitOptions.TrimEntries)
            .Any(x => x.Equals(
                "secure",
                StringComparison.OrdinalIgnoreCase))
            .Should().BeTrue();
    }

    [Fact]
    public async Task ExplicitAntiforgeryCookiePolicy_OverridesDeploymentDefault()
    {
        await using var factory = AuthServerFactory.Create(options =>
        {
            options.HubDeploymentMode = UAuthHubDeploymentMode.External;
            options.Antiforgery.CookieSameSite = SameSiteMode.Lax;
        });

        var antiforgery = factory.Services
            .GetRequiredService<IOptions<AntiforgeryOptions>>()
            .Value;

        antiforgery.Cookie.SameSite.Should().Be(SameSiteMode.Lax);
    }
}
