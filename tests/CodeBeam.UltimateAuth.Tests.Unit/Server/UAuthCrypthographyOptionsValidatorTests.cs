using AwesomeAssertions;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public sealed class UAuthCryptographyOptionsValidatorTests
{
    private static UAuthServerOptions CreateOptions(string? signingKey = null, string? hashingKey = null)
    {
        var options = new UAuthServerOptions();

        if (signingKey is not null)
            options.Cryptography.JwtSigningKey = signingKey;

        if (hashingKey is not null)
            options.Cryptography.TokenHashingKey = hashingKey;

        return options;
    }

    private static ValidateOptionsResult Validate(
        UAuthServerOptions options,
        string environment = "Development",
        bool builtInSigning = true,
        bool builtInHashing = true)
    {
        var validator = new UAuthCryptographyOptionsValidator(
            environment,
            builtInSigning,
            builtInHashing);

        return validator.Validate(null, options);
    }

    private static string NewKey()
    {
        return Convert.ToBase64String(
            RandomNumberGenerator.GetBytes(32));
    }

    private static void ShouldFail(
        ValidateOptionsResult result,
        string expectedMessage)
    {
        result.Failed.Should().BeTrue();

        result.Failures.Should()
            .Contain(x => x.Contains(
                expectedMessage,
                StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Development_AllowsDefaultKeys()
    {
        var options = new UAuthServerOptions();

        var result = Validate(options);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Production_RejectsDefaultKeys()
    {
        var options = new UAuthServerOptions();

        var result = Validate(
            options,
            Environments.Production);

        ShouldFail(result, "development key");

        result.Failures.Should().HaveCount(2);
    }

    [Fact]
    public void Production_AllowsConfiguredKeys()
    {
        var options = CreateOptions(
            NewKey(),
            NewKey());

        var result = Validate(
            options,
            Environments.Production);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void Development_AllowsConfiguredKeys()
    {
        var options = CreateOptions(
            NewKey(),
            NewKey());

        var result = Validate(options);

        result.Succeeded.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void SigningKey_RejectsMissingValue(string? value)
    {
        var options = new UAuthServerOptions();
        options.Cryptography.JwtSigningKey = value;

        var result = Validate(options);

        ShouldFail(result, "JwtSigningKey");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void HashingKey_RejectsMissingValue(string? value)
    {
        var options = new UAuthServerOptions();
        options.Cryptography.TokenHashingKey = value;

        var result = Validate(options);

        ShouldFail(result, "TokenHashingKey");
    }

    [Theory]
    [InlineData("not-base64!")]
    [InlineData("%%%")]
    public void SigningKey_RejectsInvalidBase64(string value)
    {
        var options = new UAuthServerOptions();
        options.Cryptography.JwtSigningKey = value;

        var result = Validate(options);

        ShouldFail(result, "Base64");
    }

    [Fact]
    public void HashingKey_RejectsInvalidBase64()
    {
        var options = new UAuthServerOptions();

        options.Cryptography.TokenHashingKey =
            "invalid-base64!";

        var result = Validate(options);

        ShouldFail(result, "Base64");
    }

    [Fact]
    public void SigningKey_RejectsShortKey()
    {
        var options = new UAuthServerOptions();

        options.Cryptography.JwtSigningKey =
            Convert.ToBase64String(
                RandomNumberGenerator.GetBytes(16));

        var result = Validate(options);

        ShouldFail(result, "32 bytes");
    }

    [Fact]
    public void HashingKey_RejectsShortKey()
    {
        var options = new UAuthServerOptions();

        options.Cryptography.TokenHashingKey =
            Convert.ToBase64String(
                RandomNumberGenerator.GetBytes(16));

        var result = Validate(options);

        ShouldFail(result, "32 bytes");
    }

    [Fact]
    public void Keys_RejectIdenticalValues()
    {
        var key = NewKey();

        var options = CreateOptions(key, key);

        var result = Validate(options);

        ShouldFail(result, "must be different");
    }

    [Fact]
    public void Keys_RejectAllZeroMaterial()
    {
        var options = new UAuthServerOptions();

        options.Cryptography.JwtSigningKey =
            Convert.ToBase64String(new byte[32]);

        var result = Validate(options);

        ShouldFail(result, "weak key material");
    }

    [Fact]
    public void Production_RejectsSwappedDevelopmentKeys()
    {
        var options = CreateOptions(
            UAuthCryptographyConstants.DevelopmentTokenHashingKey,
            UAuthCryptographyConstants.DevelopmentJwtSigningKey);

        var result = Validate(
            options,
            Environments.Production);

        ShouldFail(result, "development key");
        result.Failures.Should().HaveCount(2);
    }

    [Fact]
    public void CustomSigningProvider_SkipsBuiltInSigningKeyValidation()
    {
        var options = new UAuthServerOptions();

        options.Cryptography.JwtSigningKey = null;
        options.Cryptography.TokenHashingKey = NewKey();

        var result = Validate(
            options,
            Environments.Production,
            builtInSigning: false);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void CustomHasher_SkipsBuiltInHashingKeyValidation()
    {
        var options = new UAuthServerOptions();

        options.Cryptography.JwtSigningKey = NewKey();
        options.Cryptography.TokenHashingKey = null;

        var result = Validate(
            options,
            Environments.Production,
            builtInHashing: false);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void CustomProviders_SkipUnusedBuiltInKeys()
    {
        var options = new UAuthServerOptions();

        options.Cryptography.JwtSigningKey = null;
        options.Cryptography.TokenHashingKey = null;

        var result = Validate(
            options,
            Environments.Production,
            builtInSigning: false,
            builtInHashing: false);

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public void CustomSigningProvider_DoesNotDisableHashingValidation()
    {
        var options = new UAuthServerOptions();

        options.Cryptography.JwtSigningKey = null;
        options.Cryptography.TokenHashingKey = null;

        var result = Validate(
            options,
            Environments.Production,
            builtInSigning: false,
            builtInHashing: true);

        ShouldFail(result, "TokenHashingKey");
    }

    [Fact]
    public void CustomHasher_DoesNotDisableSigningValidation()
    {
        var options = new UAuthServerOptions();

        options.Cryptography.JwtSigningKey = null;
        options.Cryptography.TokenHashingKey = null;

        var result = Validate(
            options,
            Environments.Production,
            builtInSigning: true,
            builtInHashing: false);

        ShouldFail(result, "JwtSigningKey");
    }

    private sealed class TestHostEnvironment(
        string environmentName) : IHostEnvironment
    {
        public string EnvironmentName { get; set; } =
            environmentName;

        public string ApplicationName { get; set; } =
            "UltimateAuth.Tests";

        public string ContentRootPath { get; set; } =
            AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } =
            new NullFileProvider();
    }
}
