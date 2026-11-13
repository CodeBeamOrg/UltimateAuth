using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Server.Options.Validators;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server;

public sealed class UAuthArtifactStoreOptionsTests
{
    [Fact]
    public void Defaults_AreBoundedAndHaveCleanupInterval()
    {
        var options = new UAuthArtifactStoreOptions();

        Assert.Equal(50_000, options.MaxEntries);
        Assert.Equal(
            TimeSpan.FromMinutes(1),
            options.CleanupInterval);
    }

    [Fact]
    public void Clone_CreatesIndependentOptions()
    {
        var original = new UAuthArtifactStoreOptions();
        var clone = original.Clone();

        clone.MaxEntries = 500;
        clone.CleanupInterval = TimeSpan.FromMinutes(5);

        Assert.Equal(50_000, original.MaxEntries);
        Assert.Equal(
            TimeSpan.FromMinutes(1),
            original.CleanupInterval);
    }

    [Fact]
    public void ServerOptionsClone_PreservesArtifactConfiguration()
    {
        var original = new UAuthServerOptions();

        original.AuthArtifacts.MaxEntries = 25_000;
        original.AuthArtifacts.CleanupInterval =
            TimeSpan.FromSeconds(30);

        var clone = original.Clone();

        Assert.Equal(25_000, clone.AuthArtifacts.MaxEntries);
        Assert.Equal(
            TimeSpan.FromSeconds(30),
            clone.AuthArtifacts.CleanupInterval);

        clone.AuthArtifacts.MaxEntries = 50_000;

        Assert.Equal(25_000, original.AuthArtifacts.MaxEntries);
    }

    [Fact]
    public void Validator_AcceptsDefaults()
    {
        var result = Validate(new UAuthServerOptions());

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(10_000)]
    [InlineData(100_000)]
    [InlineData(1_000_000)]
    public void Validator_AcceptsPositiveCapacity(int capacity)
    {
        var options = new UAuthServerOptions();
        options.AuthArtifacts.MaxEntries = capacity;

        Assert.True(Validate(options).Succeeded);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Validator_RejectsNonPositiveCapacity(int capacity)
    {
        var options = new UAuthServerOptions();
        options.AuthArtifacts.MaxEntries = capacity;

        var result = Validate(options);

        Assert.False(result.Succeeded);
        Assert.Contains(
            "AuthArtifacts.MaxEntries",
            result.FailureMessage);
    }

    [Fact]
    public void Validator_RejectsMissingOptions()
    {
        var options = new UAuthServerOptions
        {
            AuthArtifacts = null!
        };

        var result = Validate(options);

        Assert.False(result.Succeeded);
        Assert.Contains(
            "AuthArtifacts is required",
            result.FailureMessage);
    }

    [Theory]
    [InlineData(-1000)]
    [InlineData(0)]
    [InlineData(999)]
    public void Validator_RejectsCleanupIntervalBelowOneSecond(
        int milliseconds)
    {
        var options = new UAuthServerOptions();

        options.AuthArtifacts.CleanupInterval =
            TimeSpan.FromMilliseconds(milliseconds);

        var result = Validate(options);

        Assert.False(result.Succeeded);
        Assert.Contains(
            "AuthArtifacts.CleanupInterval",
            result.FailureMessage);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(30)]
    [InlineData(60)]
    [InlineData(300)]
    public void Validator_AcceptsValidCleanupInterval(
        int seconds)
    {
        var options = new UAuthServerOptions();

        options.AuthArtifacts.CleanupInterval =
            TimeSpan.FromSeconds(seconds);

        Assert.True(Validate(options).Succeeded);
    }

    [Fact]
    public void Validator_ReportsMultipleErrors()
    {
        var options = new UAuthServerOptions();

        options.AuthArtifacts.MaxEntries = 0;
        options.AuthArtifacts.CleanupInterval = TimeSpan.Zero;

        var result = Validate(options);

        Assert.False(result.Succeeded);
        Assert.Contains(
            "AuthArtifacts.MaxEntries",
            result.FailureMessage);
        Assert.Contains(
            "AuthArtifacts.CleanupInterval",
            result.FailureMessage);
    }

    private static ValidateOptionsResult Validate(
        UAuthServerOptions options)
    {
        return new UAuthArtifactStoreOptionsValidator()
            .Validate(null, options);
    }
}
