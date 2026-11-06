using Microsoft.AspNetCore.Mvc.Testing;

namespace CodeBeam.UltimateAuth.Tests.Integration;

public class AuthServerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthServerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }
}
