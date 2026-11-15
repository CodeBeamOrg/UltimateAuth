using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Server.Options.Validators;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class DeviceLimitOptionsTests
{
    private readonly UAuthDeviceLimitOptionsValidator _validator = new();

    private ValidateOptionsResult Validate(UAuthServerOptions options) => _validator.Validate(null, options);

    [Fact]
    public void Default_options_should_have_no_device_limits()
    {
        var options = new UAuthServerOptions();

        Assert.Null(options.DeviceLimits.MaxDevices);
        Assert.Empty(options.DeviceLimits.TypeLimits);
        Assert.False(options.DeviceLimits.HasLimits);
        Assert.Equal(DeviceLimitBehavior.RejectNewLogin, options.DeviceLimits.Behavior);
    }

    [Fact]
    public void No_limits_should_pass_validation()
    {
        var result = Validate(new UAuthServerOptions());
        Assert.False(result.Failed);
    }

    [Fact]
    public void Global_limit_should_enable_limit_evaluation()
    {
        var options = new UAuthServerOptions();
        options.DeviceLimits.MaxDevices = 5;

        Assert.True(options.DeviceLimits.HasLimits);
        Assert.False(Validate(options).Failed);
    }

    [Fact]
    public void Type_limit_should_enable_limit_evaluation()
    {
        var options = new UAuthServerOptions();
        options.DeviceLimits.TypeLimits["mobile"] = 2;

        Assert.True(options.DeviceLimits.HasLimits);
        Assert.False(Validate(options).Failed);
    }

    [Fact]
    public void Global_and_type_limits_should_pass_validation()
    {
        var options = new UAuthServerOptions();

        options.DeviceLimits.MaxDevices = 5;
        options.DeviceLimits.TypeLimits["web"] = 3;
        options.DeviceLimits.TypeLimits["mobile"] = 2;
        options.DeviceLimits.Behavior =
            DeviceLimitBehavior.RevokeOldest;

        Assert.False(Validate(options).Failed);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Non_positive_global_limit_should_fail(int limit)
    {
        var options = new UAuthServerOptions();
        options.DeviceLimits.MaxDevices = limit;

        var result = Validate(options);

        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            x => x.Contains("MaxDevices must be positive"));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Non_positive_type_limit_should_fail(int limit)
    {
        var options = new UAuthServerOptions();
        options.DeviceLimits.TypeLimits["mobile"] = limit;

        var result = Validate(options);

        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            x => x.Contains("must be positive"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(" web")]
    [InlineData("web ")]
    public void Invalid_type_name_should_fail(string type)
    {
        var options = new UAuthServerOptions();
        options.DeviceLimits.TypeLimits[type] = 2;

        Assert.True(Validate(options).Failed);
    }

    [Fact]
    public void Duplicate_normalized_types_should_fail()
    {
        var options = new UAuthServerOptions();

        options.DeviceLimits.TypeLimits =
            new Dictionary<string, int>(StringComparer.Ordinal)
            {
                ["web"] = 2,
                ["WEB"] = 3
            };

        var result = Validate(options);

        Assert.True(result.Failed);
        Assert.Contains(
            result.Failures,
            x => x.Contains("duplicate device type"));
    }

    [Fact]
    public void Invalid_behavior_should_fail()
    {
        var options = new UAuthServerOptions();
        options.DeviceLimits.Behavior =
            (DeviceLimitBehavior)999;

        Assert.True(Validate(options).Failed);
    }

    [Fact]
    public void Null_type_limits_should_fail()
    {
        var options = new UAuthServerOptions();
        options.DeviceLimits.TypeLimits = null!;

        Assert.True(Validate(options).Failed);
    }

    [Fact]
    public void Null_device_limits_should_fail()
    {
        var options = new UAuthServerOptions();
        options.DeviceLimits = null!;

        Assert.True(Validate(options).Failed);
    }

    [Fact]
    public void Clone_should_preserve_values_and_isolate_dictionary()
    {
        var original = new UAuthDeviceLimitOptions
        {
            MaxDevices = 5,
            Behavior = DeviceLimitBehavior.RevokeOldest
        };

        original.TypeLimits["mobile"] = 2;

        var clone = original.Clone();

        Assert.Equal(5, clone.MaxDevices);
        Assert.Equal(
            DeviceLimitBehavior.RevokeOldest,
            clone.Behavior);
        Assert.Equal(2, clone.TypeLimits["mobile"]);

        clone.TypeLimits["mobile"] = 4;

        Assert.Equal(2, original.TypeLimits["mobile"]);
    }

    [Fact]
    public void Server_options_clone_should_isolate_device_limits()
    {
        var original = new UAuthServerOptions();
        original.DeviceLimits.MaxDevices = 5;
        original.DeviceLimits.TypeLimits["web"] = 3;

        var clone = original.Clone();

        clone.DeviceLimits.MaxDevices = 10;
        clone.DeviceLimits.TypeLimits["web"] = 7;

        Assert.Equal(5, original.DeviceLimits.MaxDevices);
        Assert.Equal(3, original.DeviceLimits.TypeLimits["web"]);
    }
}
