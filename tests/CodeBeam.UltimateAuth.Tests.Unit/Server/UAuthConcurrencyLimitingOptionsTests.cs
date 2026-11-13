using CodeBeam.UltimateAuth.Server.ConcurrencyLimiting;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server;

public sealed class UAuthConcurrencyLimitingOptionsTests
{
    [Fact]
    public void Defaults_AreCpuBasedAndBounded()
    {
        var options = new UAuthConcurrencyLimitingOptions();
        var expected = Math.Max(2, Environment.ProcessorCount);

        Assert.True(options.Enabled);

        foreach (var rule in Rules(options))
        {
            Assert.Equal(expected, rule.PermitLimit);
            Assert.Equal(expected * 5, rule.QueueLimit);
            Assert.Equal(TimeSpan.FromSeconds(3), rule.QueueTimeout);
        }
    }

    [Fact]
    public void QueueLimit_NullAndZeroArePreserved()
    {
        var rule = new UAuthConcurrencyRule();

        rule.QueueLimit = null;
        Assert.Null(rule.QueueLimit);

        rule.QueueLimit = 0;
        Assert.Equal(0, rule.QueueLimit);
    }

    [Fact]
    public void ChangingPermitLimit_DoesNotChangeQueueLimit()
    {
        var rule = new UAuthConcurrencyRule();
        var originalQueueLimit = rule.QueueLimit;

        rule.PermitLimit = 16;

        Assert.Equal(16, rule.PermitLimit);
        Assert.Equal(originalQueueLimit, rule.QueueLimit);
    }

    [Fact]
    public void Clone_CreatesIndependentNestedRules()
    {
        var original = new UAuthConcurrencyLimitingOptions();
        var clone = original.Clone();

        clone.Enabled = false;
        clone.PasswordHashing.PermitLimit = 16;
        clone.Login.QueueLimit = null;
        clone.CredentialManagement.QueueTimeout =
            TimeSpan.FromSeconds(10);

        Assert.True(original.Enabled);

        Assert.NotEqual(
            16, original.PasswordHashing.PermitLimit);

        Assert.NotNull(original.Login.QueueLimit);

        Assert.Equal(
            TimeSpan.FromSeconds(3),
            original.CredentialManagement.QueueTimeout);
    }

    [Fact]
    public void ServerOptionsClone_PreservesConcurrencyConfiguration()
    {
        var original = new UAuthServerOptions();

        original.ConcurrencyLimiting.Login.PermitLimit = 3;
        original.ConcurrencyLimiting.Login.QueueLimit = 0;

        var clone = original.Clone();

        Assert.Equal(3, clone.ConcurrencyLimiting.Login.PermitLimit);
        Assert.Equal(0, clone.ConcurrencyLimiting.Login.QueueLimit);

        clone.ConcurrencyLimiting.Login.PermitLimit = 10;

        Assert.Equal(
            3, original.ConcurrencyLimiting.Login.PermitLimit);
    }

    [Fact]
    public void Validator_AcceptsDefaults()
    {
        var result = Validate(new UAuthServerOptions());

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData("PasswordHashing")]
    [InlineData("Login")]
    [InlineData("CredentialManagement")]
    public void Validator_RejectsInvalidPermitLimit(string ruleName)
    {
        var options = new UAuthServerOptions();

        GetRule(options, ruleName).PermitLimit = 0;

        var result = Validate(options);

        Assert.False(result.Succeeded);
        Assert.Contains(
            $"ConcurrencyLimiting.{ruleName}.PermitLimit",
            result.FailureMessage);
    }

    [Theory]
    [InlineData("PasswordHashing")]
    [InlineData("Login")]
    [InlineData("CredentialManagement")]
    public void Validator_RejectsNegativeQueueLimit(string ruleName)
    {
        var options = new UAuthServerOptions();

        GetRule(options, ruleName).QueueLimit = -1;

        var result = Validate(options);

        Assert.False(result.Succeeded);
        Assert.Contains(
            $"ConcurrencyLimiting.{ruleName}.QueueLimit",
            result.FailureMessage);
    }

    [Theory]
    [InlineData("PasswordHashing")]
    [InlineData("Login")]
    [InlineData("CredentialManagement")]
    public void Validator_AcceptsUnboundedQueue(string ruleName)
    {
        var options = new UAuthServerOptions();

        GetRule(options, ruleName).QueueLimit = null;

        Assert.True(Validate(options).Succeeded);
    }

    [Theory]
    [InlineData("PasswordHashing")]
    [InlineData("Login")]
    [InlineData("CredentialManagement")]
    public void Validator_RejectsNonPositiveTimeout(string ruleName)
    {
        var options = new UAuthServerOptions();

        GetRule(options, ruleName).QueueTimeout = TimeSpan.Zero;

        Assert.False(Validate(options).Succeeded);
    }

    [Theory]
    [InlineData("PasswordHashing")]
    [InlineData("Login")]
    [InlineData("CredentialManagement")]
    public void Validator_RejectsExcessiveTimeout(string ruleName)
    {
        var options = new UAuthServerOptions();

        GetRule(options, ruleName).QueueTimeout =
            TimeSpan.FromMinutes(2);

        Assert.False(Validate(options).Succeeded);
    }

    [Fact]
    public void Validator_AcceptsDisabledConcurrencyLimiting()
    {
        var options = new UAuthServerOptions();

        options.ConcurrencyLimiting.Enabled = false;
        options.ConcurrencyLimiting.Login.PermitLimit = 0;

        Assert.True(Validate(options).Succeeded);
    }

    [Fact]
    public void Validator_RejectsMissingWorkloadRule()
    {
        var options = new UAuthServerOptions
        {
            ConcurrencyLimiting = new()
            {
                Login = null!
            }
        };

        var result = Validate(options);

        Assert.False(result.Succeeded);
        Assert.Contains(
            "ConcurrencyLimiting.Login",
            result.FailureMessage);
    }

    private static ValidateOptionsResult Validate(
        UAuthServerOptions options)
    {
        return new UAuthConcurrencyLimitingValidator()
            .Validate(null, options);
    }

    private static UAuthConcurrencyRule GetRule(
        UAuthServerOptions options,
        string name)
    {
        return name switch
        {
            "PasswordHashing" =>
                options.ConcurrencyLimiting.PasswordHashing,
            "Login" =>
                options.ConcurrencyLimiting.Login,
            "CredentialManagement" =>
                options.ConcurrencyLimiting.CredentialManagement,
            _ => throw new ArgumentOutOfRangeException(nameof(name))
        };
    }

    private static UAuthConcurrencyRule[] Rules(
        UAuthConcurrencyLimitingOptions options)
    {
        return
        [
            options.PasswordHashing,
            options.Login,
            options.CredentialManagement
        ];
    }
}
