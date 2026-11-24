using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server;

public class UAuthHubCorsOptionsSetupTests
{
    [Fact]
    public void Configure_ExposesJwtTransportHeaders()
    {
        var server = new UAuthServerOptions();

        server.Hub.AllowedClientOrigins.Add(
            "https://app.example.com");

        var setup = new UAuthHubCorsOptionsSetup(
            Options.Create(server));

        var cors = new CorsOptions();

        setup.Configure(cors);

        var policy = cors.GetPolicy("UAuthHub");

        Assert.NotNull(policy);

        Assert.Contains(
            "https://app.example.com",
            policy.Origins);

        Assert.True(policy.SupportsCredentials);

        Assert.Contains(
            "uat",
            policy.ExposedHeaders);

        Assert.Contains(
            "X-UAuth-Access-Header",
            policy.ExposedHeaders);

        Assert.Contains(
            "X-UAuth-Access-Expires",
            policy.ExposedHeaders);

        Assert.Contains(
            "X-UAuth-Access-Format",
            policy.ExposedHeaders);

        Assert.Contains(
            "X-UAuth-Access-HeaderFormat",
            policy.ExposedHeaders);

        Assert.DoesNotContain(
            "*",
            policy.Origins);
    }

    [Fact]
    public void Configure_ExposesConfiguredAccessTokenHeader()
    {
        var server = new UAuthServerOptions();

        server.Hub.AllowedClientOrigins.Add(
            "https://app.example.com");

        server.GrantDelivery.AccessToken.HeaderName =
            "X-Custom-Access";

        var setup = new UAuthHubCorsOptionsSetup(
            Options.Create(server));

        var cors = new CorsOptions();

        setup.Configure(cors);

        var policy = cors.GetPolicy("UAuthHub");

        Assert.NotNull(policy);

        Assert.Contains(
            "X-Custom-Access",
            policy.ExposedHeaders);
    }

    [Fact]
    public void Configure_WithoutAllowedOrigins_DoesNotEnableCredentials()
    {
        var setup = new UAuthHubCorsOptionsSetup(
            Options.Create(new UAuthServerOptions()));

        var cors = new CorsOptions();

        setup.Configure(cors);

        var policy = cors.GetPolicy("UAuthHub");

        Assert.NotNull(policy);
        Assert.Empty(policy.Origins);
        Assert.False(policy.SupportsCredentials);
    }
}
