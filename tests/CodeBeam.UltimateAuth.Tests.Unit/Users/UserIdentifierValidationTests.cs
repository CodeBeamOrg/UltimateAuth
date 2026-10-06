using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Users.Contracts;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public sealed class UserIdentifierValidatorTests
{
    [Fact]
    public async Task ValidateAsync_WhenIdentifierIsEmpty_ShouldReturnIdentifierEmpty()
    {
        var sut = CreateSut();

        var result = await sut.ValidateAsync(
            null!,
            new UserIdentifierInfo
            {
                Type = UserIdentifierType.Username,
                Value = "   "
            });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(x =>
            x.Code == "identifier_empty");
    }

    [Fact]
    public async Task ValidateAsync_ShouldTrimIdentifierBeforeValidation()
    {
        var sut = CreateSut();

        var identifier = new UserIdentifierInfo
        {
            Type = UserIdentifierType.Username,
            Value = "  valid_user  "
        };

        var result = await sut.ValidateAsync(null!, identifier);

        result.IsValid.Should().BeTrue();
        identifier.Value.Should().Be("valid_user");
    }

    [Fact]
    public async Task ValidateAsync_WhenUsernameIsTooShort_ShouldReturnUsernameTooShort()
    {
        var sut = CreateSut(options =>
        {
            options.IdentifierValidation.UserName.MinLength = 5;
        });

        var result = await sut.ValidateAsync(
            null!,
            new UserIdentifierInfo
            {
                Type = UserIdentifierType.Username,
                Value = "abc"
            });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x =>
            x.Code == "username_too_short");
    }

    [Fact]
    public async Task ValidateAsync_WhenUsernameIsTooLong_ShouldReturnUsernameTooLong()
    {
        var sut = CreateSut(options =>
        {
            options.IdentifierValidation.UserName.MaxLength = 5;
        });

        var result = await sut.ValidateAsync(
            null!,
            new UserIdentifierInfo
            {
                Type = UserIdentifierType.Username,
                Value = "abcdef"
            });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x =>
            x.Code == "username_too_long");
    }

    [Fact]
    public async Task ValidateAsync_WhenUsernameDoesNotMatchAllowedRegex_ShouldReturnInvalidFormat()
    {
        var sut = CreateSut(options =>
        {
            options.IdentifierValidation.UserName.AllowedRegex =
                "^[a-z]+$";
        });

        var result = await sut.ValidateAsync(
            null!,
            new UserIdentifierInfo
            {
                Type = UserIdentifierType.Username,
                Value = "user123"
            });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x =>
            x.Code == "username_invalid_format");
    }

    [Fact]
    public async Task ValidateAsync_WhenUsernameValidationIsDisabled_ShouldSkipUsernameRules()
    {
        var sut = CreateSut(options =>
        {
            options.IdentifierValidation.UserName.Enabled = false;
            options.IdentifierValidation.UserName.MinLength = 100;
        });

        var result = await sut.ValidateAsync(
            null!,
            new UserIdentifierInfo
            {
                Type = UserIdentifierType.Username,
                Value = "a"
            });

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task ValidateAsync_WhenEmailDoesNotContainAt_ShouldReturnInvalidFormat()
    {
        var sut = CreateSut();

        var result = await sut.ValidateAsync(
            null!,
            new UserIdentifierInfo
            {
                Type = UserIdentifierType.Email,
                Value = "not-an-email"
            });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(x =>
            x.Code == "email_invalid_format");
    }

    [Fact]
    public async Task ValidateAsync_WhenEmailIsTooShort_ShouldReturnEmailTooShort()
    {
        var sut = CreateSut(options =>
        {
            options.IdentifierValidation.Email.MinLength = 10;
        });

        var result = await sut.ValidateAsync(
            null!,
            new UserIdentifierInfo
            {
                Type = UserIdentifierType.Email,
                Value = "a@b.co"
            });

        result.Errors.Should().Contain(x =>
            x.Code == "email_too_short");
    }

    [Fact]
    public async Task ValidateAsync_WhenEmailIsTooLong_ShouldReturnEmailTooLong()
    {
        var sut = CreateSut(options =>
        {
            options.IdentifierValidation.Email.MaxLength = 5;
        });

        var result = await sut.ValidateAsync(
            null!,
            new UserIdentifierInfo
            {
                Type = UserIdentifierType.Email,
                Value = "abc@example.com"
            });

        result.Errors.Should().Contain(x =>
            x.Code == "email_too_long");
    }

    [Fact]
    public async Task ValidateAsync_WhenPhoneIsTooShort_ShouldReturnPhoneTooShort()
    {
        var sut = CreateSut(options =>
        {
            options.IdentifierValidation.Phone.MinLength = 10;
        });

        var result = await sut.ValidateAsync(
            null!,
            new UserIdentifierInfo
            {
                Type = UserIdentifierType.Phone,
                Value = "123"
            });

        result.Errors.Should().Contain(x =>
            x.Code == "phone_too_short");
    }

    [Fact]
    public async Task ValidateAsync_WhenPhoneIsTooLong_ShouldReturnPhoneTooLong()
    {
        var sut = CreateSut(options =>
        {
            options.IdentifierValidation.Phone.MaxLength = 5;
        });

        var result = await sut.ValidateAsync(
            null!,
            new UserIdentifierInfo
            {
                Type = UserIdentifierType.Phone,
                Value = "123456789"
            });

        result.Errors.Should().Contain(x =>
            x.Code == "phone_too_long");
    }

    [Fact]
    public async Task ValidateAsync_WhenCancellationRequested_ShouldThrow()
    {
        var sut = CreateSut();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => sut.ValidateAsync(
            null!,
            new UserIdentifierInfo
            {
                Type = UserIdentifierType.Username,
                Value = "valid_user"
            },
            cts.Token);

        await act.Should()
            .ThrowAsync<OperationCanceledException>();
    }

    private static UserIdentifierValidator CreateSut(
        Action<UAuthServerOptions>? configure = null)
    {
        var options = new UAuthServerOptions();

        configure?.Invoke(options);

        return new UserIdentifierValidator(
            Options.Create(options));
    }
}
