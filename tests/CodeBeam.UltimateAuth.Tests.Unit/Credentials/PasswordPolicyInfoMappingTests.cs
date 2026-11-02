using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Options;

namespace CodeBeam.UltimateAuth.Tests.Unit.Credentials;

public sealed class PasswordPolicyInfoMappingTests
{
    [Fact]
    public void StandardPolicy_MapsExpectedRequirements()
    {
        var effective = PasswordPolicyOptionsResolver.Resolve(
            new PasswordPolicyOptions());

        var info = effective.ToInfo();

        Assert.Equal(PasswordPolicyLevel.Standard, info.Level);
        Assert.Equal(8, info.MinLength);
        Assert.Equal(128, info.MaxLength);
        Assert.True(info.RequireLetter);
        Assert.True(info.RequireDigit);
        Assert.False(info.RequireUppercase);
        Assert.False(info.RequireLowercase);
        Assert.False(info.RequireSpecialCharacter);
    }

    [Fact]
    public void StrictOverrides_ArePreserved()
    {
        var effective = PasswordPolicyOptionsResolver.Resolve(
            new PasswordPolicyOptions
            {
                Level = PasswordPolicyLevel.Strict,
                MinLength = 12,
                MaxLength = 64,
                RequireDigit = false
            });

        var info = effective.ToInfo();

        Assert.Equal(PasswordPolicyLevel.Strict, info.Level);
        Assert.Equal(12, info.MinLength);
        Assert.Equal(64, info.MaxLength);
        Assert.False(info.RequireDigit);
        Assert.True(info.RequireUppercase);
        Assert.True(info.RequireLowercase);
        Assert.True(info.RequireSpecialCharacter);
    }

    [Fact]
    public void OptionalChecks_AreMapped()
    {
        var effective = PasswordPolicyOptionsResolver.Resolve(
            new PasswordPolicyOptions
            {
                CommonPasswords = PasswordPolicyLevel.Disabled,
                UserSimilarity = PasswordPolicyLevel.Strict,
                History = new PasswordHistoryOptions
                {
                    RememberLast = 5
                }
            });

        var info = effective.ToInfo();

        Assert.False(info.CheckCommonPasswords);
        Assert.True(info.CheckUserSimilarity);
        Assert.True(info.CheckPasswordHistory);
    }
}
