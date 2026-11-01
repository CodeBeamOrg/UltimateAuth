using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Options;
using CodeBeam.UltimateAuth.Credentials.Policies;

public sealed class DefaultPasswordStrengthPolicyTests
{
    private readonly IPasswordStrengthPolicy _policy = new DefaultPasswordStrengthPolicy(new BuiltInCommonPasswordChecker(), new DefaultPasswordUserSimilarityChecker());

    private static EffectivePasswordPolicyOptions Resolve(PasswordPolicyLevel level = PasswordPolicyLevel.Standard) 
        => PasswordPolicyOptionsResolver.Resolve(new PasswordPolicyOptions { Level = level });

    private Task<PasswordPolicyResult> ValidateAsync(string password, PasswordPolicyLevel level = PasswordPolicyLevel.Standard, string? userName = null)
    {
        return _policy.ValidateAsync(
            new PasswordPolicyContext
            {
                Password = password,
                UserName = userName
            },
            Resolve(level));
    }

    [Fact]
    public async Task Standard_RequiresLetterAndDigit()
    {
        var result = await ValidateAsync("abcdefgh");

        Assert.False(result.Succeeded);
        Assert.Contains(
            result.Violations,
            x => x.Code == PasswordPolicyErrorCodes.DigitRequired);
    }

    [Fact]
    public async Task Basic_AllowsPasswordWithoutDigits()
    {
        var result = await ValidateAsync(
            "longpassphrase",
            PasswordPolicyLevel.Basic);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task Standard_RejectsCommonPassword()
    {
        var result = await ValidateAsync("password123");

        Assert.Contains(
            result.Violations,
            x => x.Code == PasswordPolicyErrorCodes.CommonPassword);
    }

    [Fact]
    public async Task Standard_RejectsUsernameInPassword()
    {
        var result = await ValidateAsync(
            "JohnSmith123!",
            userName: "johnsmith");

        Assert.Contains(
            result.Violations,
            x => x.Code == PasswordPolicyErrorCodes.SimilarToUser);
    }

    [Fact]
    public async Task Strict_RejectsNormalizedUsername()
    {
        var result = await ValidateAsync(
            "J0hn-Smith123!",
            PasswordPolicyLevel.Strict,
            "johnsmith");

        Assert.Contains(
            result.Violations,
            x => x.Code == PasswordPolicyErrorCodes.SimilarToUser);
    }

    [Fact]
    public async Task Strict_RequiresAllConfiguredClasses()
    {
        var result = await ValidateAsync(
            "longpassword123",
            PasswordPolicyLevel.Strict);

        Assert.Contains(
            result.Violations,
            x => x.Code == PasswordPolicyErrorCodes.UppercaseRequired);

        Assert.Contains(
            result.Violations,
            x => x.Code == PasswordPolicyErrorCodes.SpecialCharacterRequired);
    }

    [Fact]
    public async Task EmptyPassword_ReturnsRequiredViolation()
    {
        var result = await ValidateAsync("");

        Assert.Contains(
            result.Violations,
            x => x.Code == PasswordPolicyErrorCodes.Required);
    }

    [Fact]
    public async Task ExcessiveInput_IsRejectedBeforeOtherChecks()
    {
        var result = await ValidateAsync(new string('a', 257));

        Assert.Single(result.Violations);
        Assert.Equal(
            PasswordPolicyErrorCodes.InputTooLong,
            result.Violations[0].Code);
    }

    [Fact]
    public async Task ValidStandardPassword_Succeeds()
    {
        var result = await ValidateAsync("BlueRiver4827!");

        Assert.True(result.Succeeded);
        Assert.Empty(result.Violations);
    }
}
