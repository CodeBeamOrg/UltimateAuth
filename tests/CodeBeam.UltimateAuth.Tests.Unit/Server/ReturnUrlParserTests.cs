using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public sealed class ReturnUrlParserTests
{
    [Fact]
    public void Relative_Path_Is_Not_Treated_As_Absolute_Uri()
    {
        var input = "/dashboard";
        var info = ReturnUrlParser.Parse(input);

        info.Should().NotBeNull();
        info!.IsAbsolute.Should().BeFalse();
        info.RelativePath.Should().Be("/dashboard");
        info.AbsoluteUri.Should().BeNull();
    }

    [Fact]
    public void Absolute_Https_Url_Is_Treated_As_Absolute()
    {
        var input = "https://app.example.com/dashboard";
        var info = ReturnUrlParser.Parse(input);

        info.Should().NotBeNull();
        info!.IsAbsolute.Should().BeTrue();
        info.AbsoluteUri!.ToString().Should().Be(input);
        info.RelativePath.Should().BeNull();
    }

    [Theory]
    [InlineData("javascript:alert(1)")]
    [InlineData("data:text/html;base64,AAA")]
    [InlineData("file:///etc/passwd")]
    [InlineData("ftp://evil.com")]
    public void Parser_Rejects_Unsafe_Schemes(string returnUrl)
    {
        Action act = () => ReturnUrlParser.Parse(returnUrl);
        act.Should().Throw<InvalidOperationException>().WithMessage("*Invalid returnUrl*");
    }
}
