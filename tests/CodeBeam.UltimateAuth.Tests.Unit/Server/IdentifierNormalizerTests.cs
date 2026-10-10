using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Users.Contracts;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class IdentifierNormalizerTests
{
    [Theory]
    [InlineData(UserIdentifierType.Email)]
    [InlineData(UserIdentifierType.Phone)]
    [InlineData(UserIdentifierType.Username)]
    [InlineData((UserIdentifierType)999)]
    public void Empty_identifier_should_be_rejected(UserIdentifierType type)
    {
        var normalizer = CreateNormalizer();

        foreach (var input in new[] { "", " ", "\t\r\n" })
        {
            var result = normalizer.Normalize(type, input);

            result.IsValid.Should().BeFalse();
            result.ErrorCode.Should().Be("identifier_empty");
            result.Normalized.Should().BeEmpty();
            result.Raw.Should().Be(input);
        }
    }

    [Fact]
    public void Null_identifier_should_be_rejected()
    {
        var normalizer = CreateNormalizer();

        var result = normalizer.Normalize(
            UserIdentifierType.Username,
            null!);

        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be("identifier_empty");
        result.Normalized.Should().BeEmpty();
    }

    [Fact]
    public void Basic_normalization_should_trim_and_apply_nfkc()
    {
        var normalizer = CreateNormalizer();

        var result = normalizer.Normalize(
            UserIdentifierType.Username,
            "  Ａｌｉ１２３  ");

        result.IsValid.Should().BeTrue();
        result.Normalized.Should().Be("ali123");
        result.Raw.Should().Be("  Ａｌｉ１２３  ");
    }

    [Fact]
    public void Basic_normalization_should_remove_control_and_zero_width_characters()
    {
        var normalizer = CreateNormalizer();

        var result = normalizer.Normalize(
            UserIdentifierType.Username,
            "ab\u200Bc\u200Cd\u200De\uFEFFf\u0001");

        result.IsValid.Should().BeTrue();
        result.Normalized.Should().Be("abcdef");
    }

    [Theory]
    [InlineData("ab", "username_invalid_length")]
    [InlineData("a", "username_invalid_length")]
    public void Username_should_reject_short_values(
        string input,
        string expectedError)
    {
        var normalizer = CreateNormalizer();

        var result = normalizer.Normalize(
            UserIdentifierType.Username,
            input);

        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be(expectedError);
    }

    [Fact]
    public void Username_should_accept_minimum_length()
    {
        var result = CreateNormalizer().Normalize(
            UserIdentifierType.Username,
            "abc");

        result.IsValid.Should().BeTrue();
        result.Normalized.Should().Be("abc");
    }

    [Fact]
    public void Username_should_accept_maximum_length()
    {
        var input = new string('a', 256);

        var result = CreateNormalizer().Normalize(
            UserIdentifierType.Username,
            input);

        result.IsValid.Should().BeTrue();
        result.Normalized.Should().HaveLength(256);
    }

    [Fact]
    public void Username_should_reject_more_than_maximum_length()
    {
        var result = CreateNormalizer().Normalize(
            UserIdentifierType.Username,
            new string('a', 257));

        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be("username_invalid_length");
    }

    [Theory]
    [InlineData(CaseHandling.ToLower, "mixeduser")]
    [InlineData(CaseHandling.ToUpper, "MIXEDUSER")]
    public void Username_should_apply_case_policy(
        CaseHandling policy,
        string expected)
    {
        var normalizer = CreateNormalizer(o =>
        {
            o.Identifiers.Normalization.UsernameCase = policy;
        });

        var result = normalizer.Normalize(
            UserIdentifierType.Username,
            "MixedUser");

        result.IsValid.Should().BeTrue();
        result.Normalized.Should().Be(expected);
    }

    [Fact]
    public void Username_should_preserve_case_when_policy_is_not_lower_or_upper()
    {
        var normalizer = CreateNormalizer(o =>
        {
            o.Identifiers.Normalization.UsernameCase = (CaseHandling)999;
        });

        var result = normalizer.Normalize(
            UserIdentifierType.Username,
            "MixedUser");

        result.Normalized.Should().Be("MixedUser");
    }

    [Theory]
    [InlineData("plainaddress")]
    [InlineData("@example.com")]
    [InlineData("a@@example.com")]
    [InlineData("a@b@example.com")]
    public void Email_should_reject_invalid_at_sign_structure(string input)
    {
        var result = CreateNormalizer().Normalize(
            UserIdentifierType.Email,
            input);

        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be("email_invalid_format");
    }

    [Theory]
    [InlineData("user@")]
    [InlineData("user@example")]
    [InlineData("user@   ")]
    public void Email_should_reject_missing_or_invalid_domain(string input)
    {
        var result = CreateNormalizer().Normalize(
            UserIdentifierType.Email,
            input);

        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be("email_invalid_domain");
    }

    [Fact]
    public void Email_should_convert_internationalized_domain_to_ascii()
    {
        var result = CreateNormalizer().Normalize(
            UserIdentifierType.Email,
            "User@bücher.de");

        result.IsValid.Should().BeTrue();
        result.Normalized.Should().Be("user@xn--bcher-kva.de");
    }

    [Fact]
    public void Email_should_reject_domain_when_idn_conversion_fails()
    {
        var result = CreateNormalizer().Normalize(
            UserIdentifierType.Email,
            "user@bad..example");

        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be("email_invalid_domain");
    }

    [Theory]
    [InlineData(CaseHandling.ToLower, "user@example.com")]
    [InlineData(CaseHandling.ToUpper, "USER@EXAMPLE.COM")]
    public void Email_should_apply_case_policy(
        CaseHandling policy,
        string expected)
    {
        var normalizer = CreateNormalizer(o =>
        {
            o.Identifiers.Normalization.EmailCase = policy;
        });

        var result = normalizer.Normalize(
            UserIdentifierType.Email,
            "User@Example.COM");

        result.IsValid.Should().BeTrue();
        result.Normalized.Should().Be(expected);
    }

    [Fact]
    public void Email_should_preserve_case_when_policy_is_not_lower_or_upper()
    {
        var normalizer = CreateNormalizer(o =>
        {
            o.Identifiers.Normalization.EmailCase = (CaseHandling)999;
        });

        var result = normalizer.Normalize(
            UserIdentifierType.Email,
            "User@Example.COM");

        result.IsValid.Should().BeTrue();
        result.Normalized.Should().Be("User@Example.COM");
    }

    [Theory]
    [InlineData("+90 (555) 123-45-67", "+905551234567")]
    [InlineData("0555 123 45 67", "05551234567")]
    [InlineData("123-4567", "1234567")]
    public void Phone_should_remove_formatting_characters(
        string input,
        string expected)
    {
        var result = CreateNormalizer().Normalize(
            UserIdentifierType.Phone,
            input);

        result.IsValid.Should().BeTrue();
        result.Normalized.Should().Be(expected);
    }

    [Theory]
    [InlineData("123456")]
    [InlineData("abc")]
    [InlineData("++")]
    public void Phone_should_reject_short_normalized_values(string input)
    {
        var result = CreateNormalizer().Normalize(
            UserIdentifierType.Phone,
            input);

        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be("phone_invalid_length");
    }

    [Fact]
    public void Phone_should_accept_seven_character_normalized_value()
    {
        var result = CreateNormalizer().Normalize(
            UserIdentifierType.Phone,
            "1234567");

        result.IsValid.Should().BeTrue();
        result.Normalized.Should().Be("1234567");
    }

    [Fact]
    public void Phone_should_keep_plus_only_at_beginning_of_output()
    {
        var result = CreateNormalizer().Normalize(
            UserIdentifierType.Phone,
            "12+34+567");

        result.IsValid.Should().BeTrue();
        result.Normalized.Should().Be("1234567");
    }

    [Fact]
    public void Phone_should_accept_unicode_digits()
    {
        var result = CreateNormalizer().Normalize(
            UserIdentifierType.Phone,
            "١٢٣٤٥٦٧");

        result.IsValid.Should().BeTrue();
        result.Normalized.Should().Be("١٢٣٤٥٦٧");
    }

    [Theory]
    [InlineData(CaseHandling.ToLower, "customvalue")]
    [InlineData(CaseHandling.ToUpper, "CUSTOMVALUE")]
    public void Custom_identifier_should_apply_case_policy(
        CaseHandling policy,
        string expected)
    {
        var normalizer = CreateNormalizer(o =>
        {
            o.Identifiers.Normalization.CustomCase = policy;
        });

        var result = normalizer.Normalize(
            (UserIdentifierType)999,
            "CustomValue");

        result.IsValid.Should().BeTrue();
        result.Normalized.Should().Be(expected);
    }

    [Fact]
    public void Custom_identifier_should_preserve_case_for_unknown_policy()
    {
        var normalizer = CreateNormalizer(o =>
        {
            o.Identifiers.Normalization.CustomCase = (CaseHandling)999;
        });

        var result = normalizer.Normalize(
            (UserIdentifierType)999,
            "CustomValue");

        result.Normalized.Should().Be("CustomValue");
    }

    [Fact]
    public void Custom_identifier_should_reject_value_that_becomes_empty()
    {
        var result = CreateNormalizer().Normalize(
            (UserIdentifierType)999,
            "\u200B\u200C\u200D\uFEFF");

        result.IsValid.Should().BeFalse();
        result.Normalized.Should().BeEmpty();
        result.ErrorCode.Should().Be("identifier_invalid");
    }

    private static IdentifierNormalizer CreateNormalizer(
        Action<UAuthServerOptions>? configure = null)
    {
        var options = new UAuthServerOptions();

        configure?.Invoke(options);

        return new IdentifierNormalizer(Options.Create(options));
    }
}
