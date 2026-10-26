using AwesomeAssertions;
using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Options;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class UAuthClientResourceOptionsValidatorTests
{
    private readonly UAuthClientResourceOptionsValidator _validator = new();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("https://api.example.com")]
    [InlineData("https://api.example.com/")]
    [InlineData("http://localhost:6120")]
    [InlineData("http://127.0.0.1:6120")]
    public void Valid_BaseAddress_Should_Pass(string? baseAddress)
    {
        var options = CreateOptions(baseAddress);

        var result = _validator.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData("/api")]
    [InlineData("api.example.com")]
    [InlineData("ftp://api.example.com")]
    [InlineData("http://api.example.com")]
    [InlineData("https://api.example.com/path")]
    [InlineData("https://api.example.com?key=value")]
    [InlineData("https://api.example.com#fragment")]
    [InlineData("https://user:password@api.example.com")]
    public void Invalid_BaseAddress_Should_Fail(string baseAddress)
    {
        var options = CreateOptions(baseAddress);

        var result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
    }

    [Fact]
    public void Null_Resource_Options_Should_Fail()
    {
        var options = new UAuthClientOptions
        {
            Resource = null!
        };

        var result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
    }

    [Fact]
    public void Invalid_Credentials_Mode_Should_Fail()
    {
        var options = CreateOptions("https://api.example.com");

        options.Resource.Credentials =
            (UAuthResourceCredentialsMode)999;

        var result = _validator.Validate(null, options);

        result.Failed.Should().BeTrue();
    }

    [Theory]
    [InlineData(UAuthResourceCredentialsMode.Auto)]
    [InlineData(UAuthResourceCredentialsMode.SameOrigin)]
    [InlineData(UAuthResourceCredentialsMode.Include)]
    [InlineData(UAuthResourceCredentialsMode.Omit)]
    public void Valid_Credentials_Mode_Should_Pass(
        UAuthResourceCredentialsMode credentials)
    {
        var options = CreateOptions("https://api.example.com");
        options.Resource.Credentials = credentials;

        var result = _validator.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

    private static UAuthClientOptions CreateOptions(string? baseAddress)
        => new()
        {
            Resource = new UAuthClientResourceOptions
            {
                BaseAddress = baseAddress
            }
        };
}
