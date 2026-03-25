using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestPlatform.TestHost;

namespace CodeBeam.UltimateAuth.Tests.Integration;

public class AuthServerTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public AuthServerTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }
}
