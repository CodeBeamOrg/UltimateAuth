using CodeBeam.UltimateAuth.Tests.Integration.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;

public sealed class UAuthHubSampleSmokeTests : IClassFixture<AuthServerFactory>
{
    private readonly HttpClient _client;

    public UAuthHubSampleSmokeTests(AuthServerFactory factory)
    {
        _client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false
            });
    }

    [Theory]
    [InlineData("/")]
    [InlineData("/login")]
    public async Task CriticalPages_ShouldRenderWithoutServerError(string path)
    {
        var response = await _client.GetAsync(path);

        ((int)response.StatusCode)
            .Should()
            .BeLessThan(500);
    }
}