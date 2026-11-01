using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Options;
using CodeBeam.UltimateAuth.Server.Options;

namespace CodeBeam.UltimateAuth.Tests.Unit.Credentials;

public sealed class PasswordPolicyOptionsTests
{
    private readonly UAuthServerPasswordPolicyOptionsValidator _validator = new();

    [Fact]
    public void DefaultOptions_ResolveToStandard()
    {
        var effective = PasswordPolicyOptionsResolver.Resolve(new());

        Assert.Equal(PasswordPolicyLevel.Standard, effective.Level);
        Assert.Equal(8, effective.MinLength);
        Assert.Equal(128, effective.MaxLength);
        Assert.True(effective.RequireLetter);
        Assert.True(effective.RequireDigit);
        Assert.False(effective.RequireSpecialCharacter);
        Assert.Equal(0, effective.RememberLast);
    }

    [Fact]
    public void BasicPreset_DoesNotRequireCharacterClasses()
    {
        var effective = PasswordPolicyOptionsResolver.Resolve(
            new PasswordPolicyOptions
            {
                Level = PasswordPolicyLevel.Basic
            });

        Assert.False(effective.RequireLetter);
        Assert.False(effective.RequireDigit);
        Assert.False(effective.RequireUppercase);
        Assert.False(effective.RequireSpecialCharacter);
    }

    [Fact]
    public void Overrides_TakePrecedenceOverPreset()
    {
        var effective = PasswordPolicyOptionsResolver.Resolve(
            new PasswordPolicyOptions
            {
                Level = PasswordPolicyLevel.Strict,
                MinLength = 12,
                RequireDigit = false,
                CommonPasswords = PasswordPolicyLevel.Basic
            });

        Assert.Equal(12, effective.MinLength);
        Assert.False(effective.RequireDigit);
        Assert.True(effective.RequireUppercase);
        Assert.Equal(PasswordPolicyLevel.Basic, effective.CommonPasswords);
    }

    [Theory]
    [InlineData(PasswordPolicyLevel.Disabled)]
    [InlineData((PasswordPolicyLevel)999)]
    public void InvalidGlobalLevel_IsRejected(PasswordPolicyLevel level)
    {
        var result = _validator.Validate(
            null,
            new UAuthServerOptions
            {
                Password = new PasswordPolicyOptions { Level = level }
            });

        Assert.True(result.Failed);
    }

    [Theory]
    [InlineData(5, 128)]
    [InlineData(8, 257)]
    [InlineData(150, 128)]
    public void InvalidLengths_AreRejected(int min, int max)
    {
        var result = _validator.Validate(
            null,
            new UAuthServerOptions
            {
                Password = new PasswordPolicyOptions
                {
                    MinLength = min,
                    MaxLength = max
                }
            });

        Assert.True(result.Failed);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(11)]
    public void InvalidHistoryCount_IsRejected(int count)
    {
        var result = _validator.Validate(
            null,
            new UAuthServerOptions
            {
                Password = new PasswordPolicyOptions
                {
                    History = new PasswordHistoryOptions
                    {
                        RememberLast = count
                    }
                }
            });

        Assert.True(result.Failed);
    }

    [Fact]
    public void DisabledIndividualCheck_IsAllowed()
    {
        var options = new PasswordPolicyOptions
        {
            UserSimilarity = PasswordPolicyLevel.Disabled
        };

        var result = _validator.Validate(null, new UAuthServerOptions { Password = options });

        Assert.True(result.Succeeded);
    }
}
