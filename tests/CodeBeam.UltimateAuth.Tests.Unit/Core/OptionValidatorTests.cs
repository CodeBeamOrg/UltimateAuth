using CodeBeam.UltimateAuth.Core.Options;
using FluentAssertions;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class OptionValidatorTests
{
    [Fact]
    public void Negative_max_failed_attempts_should_fail_validation()
    {
        var options = new UAuthLoginOptions
        {
            MaxFailedAttempts = -1
        };

        var validator = new UAuthLoginOptionsValidator();

        var result = validator.Validate(null, options);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public void Excessive_max_failed_attempts_should_fail_validation()
    {
        var options = new UAuthLoginOptions
        {
            MaxFailedAttempts = 1000
        };

        var validator = new UAuthLoginOptionsValidator();

        var result = validator.Validate(null, options);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public void Lockout_enabled_without_duration_should_fail()
    {
        var options = new UAuthLoginOptions
        {
            MaxFailedAttempts = 3,
            LockoutMinutes = 0
        };

        var validator = new UAuthLoginOptionsValidator();

        var result = validator.Validate(null, options);

        result.Succeeded.Should().BeFalse();
    }

    [Fact]
    public void Lockout_disabled_should_allow_zero_duration()
    {
        var options = new UAuthLoginOptions
        {
            MaxFailedAttempts = 0,
            LockoutMinutes = 0
        };

        var validator = new UAuthLoginOptionsValidator();

        var result = validator.Validate(null, options);

        result.Succeeded.Should().BeTrue();
    }

}
