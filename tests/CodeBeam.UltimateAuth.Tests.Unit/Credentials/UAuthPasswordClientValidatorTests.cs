using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Credentials.Contracts;

namespace CodeBeam.UltimateAuth.Tests.Unit.Credentials;

public sealed class UAuthPasswordClientValidatorTests
{
    [Fact]
    public void ValidPassword_ReturnsNoViolations()
    {
        var errors = UAuthPasswordClientValidator.Validate(
            "SecurePass123!",
            CreatePolicy());

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData("", PasswordPolicyErrorCodes.Required)]
    [InlineData("Ab1!", PasswordPolicyErrorCodes.TooShort)]
    [InlineData("abcdefgh!", PasswordPolicyErrorCodes.DigitRequired)]
    [InlineData("ABCDEFGH1!", PasswordPolicyErrorCodes.LowercaseRequired)]
    [InlineData("abcdefgh1!", PasswordPolicyErrorCodes.UppercaseRequired)]
    [InlineData("Abcdefgh12", PasswordPolicyErrorCodes.SpecialCharacterRequired)]
    public void InvalidPassword_ReturnsExpectedCode(string password, string expectedCode)
    {
        var errors = UAuthPasswordClientValidator.Validate(password, CreatePolicy());

        Assert.Contains(expectedCode, errors);
    }

    [Fact]
    public void TooLongPassword_ReturnsLengthViolation()
    {
        var errors = UAuthPasswordClientValidator.Validate(
            new string('A', 129),
            CreatePolicy());

        Assert.Contains(PasswordPolicyErrorCodes.TooLong, errors);
    }

    [Fact]
    public void AbsoluteInputLimit_IsEnforced()
    {
        var errors = UAuthPasswordClientValidator.Validate(
            new string('A', 257),
            CreatePolicy());

        Assert.Contains(
            PasswordPolicyErrorCodes.InputTooLong,
            errors);
    }

    [Fact]
    public void DisabledCharacterRequirements_AreNotEnforced()
    {
        var policy = CreatePolicy(
            level: PasswordPolicyLevel.Basic,
            minLength: 6,
            requireLetter: false,
            requireDigit: false,
            requireUppercase: false,
            requireLowercase: false,
            requireSpecialCharacter: false);

        var errors = UAuthPasswordClientValidator.Validate(
            "abcdefgh",
            policy);

        Assert.Empty(errors);
    }

    [Fact]
    public void ServerOnlyChecks_AreNotEvaluatedLocally()
    {
        var policy = CreatePolicy(
            level: PasswordPolicyLevel.Standard,
            requireUppercase: false,
            requireLowercase: false,
            requireSpecialCharacter: false,
            checkCommonPasswords: true,
            checkUserSimilarity: true,
            checkPasswordHistory: true);

        var errors = UAuthPasswordClientValidator.Validate(
            "qwerty123",
            policy);

        Assert.Empty(errors);
    }

    private static UAuthPasswordPolicyInfo CreatePolicy(
        PasswordPolicyLevel level = PasswordPolicyLevel.Strict,
        int minLength = 8,
        int maxLength = 128,
        bool requireLetter = true,
        bool requireDigit = true,
        bool requireUppercase = true,
        bool requireLowercase = true,
        bool requireSpecialCharacter = true,
        bool checkCommonPasswords = false,
        bool checkUserSimilarity = false,
        bool checkPasswordHistory = false)
    {
        return new UAuthPasswordPolicyInfo
        {
            Level = level,
            MinLength = minLength,
            MaxLength = maxLength,
            RequireLetter = requireLetter,
            RequireDigit = requireDigit,
            RequireUppercase = requireUppercase,
            RequireLowercase = requireLowercase,
            RequireSpecialCharacter = requireSpecialCharacter,
            CheckCommonPasswords = checkCommonPasswords,
            CheckUserSimilarity = checkUserSimilarity,
            CheckPasswordHistory = checkPasswordHistory
        };
    }

}
