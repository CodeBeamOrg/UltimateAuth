using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class ClientBaseAddressProviderTests
{
    [Fact]
    public void Resolve_Uses_Absolute_ReturnUrl()
    {
        var resolver = TestClientBaseAddressResolver.Create();
        var ctx = TestHttpContext.Create().WithReturnUrl("https://app.example.com/dashboard");

        var options = new UAuthServerOptions();

        var result = resolver.Resolve(ctx, options, "https://app.example.com/dashboard");

        result.Should().Be("https://app.example.com");
    }

    [Fact]
    public void Resolve_Ignores_Relative_ReturnUrl()
    {
        var resolver = TestClientBaseAddressResolver.Create();

        var ctx = TestHttpContext
            .Create()
            .WithReturnUrl("/dashboard");

        var options = new UAuthServerOptions
        {
            Hub = { ClientBaseAddress = "https://fallback.example.com" }
        };

        var result = resolver.Resolve(ctx, options, "/dashboard");

        result.Should().Be("https://fallback.example.com");
    }

    [Fact]
    public void Resolve_Fails_When_Origin_Not_Allowed()
    {
        var resolver = TestClientBaseAddressResolver.Create();
        var ctx = TestHttpContext.Create().WithHeader("Origin", "https://evil.com");

        var options = new UAuthServerOptions
        {
            Hub =
        {
            AllowedClientOrigins = new HashSet<string>
            {
                "https://app.example.com"
            }
        }
        };

        Action act = () => resolver.Resolve(ctx, options, "https://evil.com");

        act.Should().Throw<InvalidOperationException>().WithMessage("*not allowed*");
    }

}
