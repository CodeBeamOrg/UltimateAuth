using AwesomeAssertions;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public sealed class UserProfileSafetyValidationTests
{
    [Theory]
    [InlineData(nameof(UserProfileInfo.FirstName), UserProfileLimits.FirstName)]
    [InlineData(nameof(UserProfileInfo.LastName), UserProfileLimits.LastName)]
    [InlineData(nameof(UserProfileInfo.DisplayName), UserProfileLimits.DisplayName)]
    [InlineData(nameof(UserProfileInfo.Gender), UserProfileLimits.Gender)]
    [InlineData(nameof(UserProfileInfo.Bio), UserProfileLimits.Bio)]
    [InlineData(nameof(UserProfileInfo.Language), UserProfileLimits.Language)]
    [InlineData(nameof(UserProfileInfo.TimeZone), UserProfileLimits.TimeZone)]
    [InlineData(nameof(UserProfileInfo.Culture), UserProfileLimits.Culture)]
    public void ProfileFields_AtLimit_AreAccepted(string property, int maxLength)
    {
        var profile = new UserProfileInfo();

        typeof(UserProfileInfo)
            .GetProperty(property)!
            .SetValue(profile, new string('x', maxLength));

        var errors = UserProfileSafetyValidation.Validate(profile);

        errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData(nameof(UserProfileInfo.FirstName), UserProfileLimits.FirstName)]
    [InlineData(nameof(UserProfileInfo.LastName), UserProfileLimits.LastName)]
    [InlineData(nameof(UserProfileInfo.DisplayName), UserProfileLimits.DisplayName)]
    [InlineData(nameof(UserProfileInfo.Gender), UserProfileLimits.Gender)]
    [InlineData(nameof(UserProfileInfo.Bio), UserProfileLimits.Bio)]
    [InlineData(nameof(UserProfileInfo.Language), UserProfileLimits.Language)]
    [InlineData(nameof(UserProfileInfo.TimeZone), UserProfileLimits.TimeZone)]
    [InlineData(nameof(UserProfileInfo.Culture), UserProfileLimits.Culture)]
    public void ProfileFields_OverLimit_AreRejected(string property, int maxLength)
    {
        var profile = new UserProfileInfo();

        typeof(UserProfileInfo)
            .GetProperty(property)!
            .SetValue(profile, new string('x', maxLength + 1));

        var errors = UserProfileSafetyValidation.Validate(profile);

        errors.Should().ContainSingle()
            .Which.Code.Should().Be("profile_field_too_long");
    }

    [Fact]
    public void Metadata_TooManyEntries_IsRejected()
    {
        var metadata = Enumerable
            .Range(0, UserProfileLimits.MetadataEntries + 1)
            .ToDictionary(x => $"key{x}", _ => "value");

        var profile = new UserProfileInfo
        {
            Metadata = metadata
        };

        var errors = UserProfileSafetyValidation.Validate(profile);

        errors.Should().Contain(
            x => x.Code == "profile_metadata_entries_exceeded");
    }

    [Fact]
    public void Metadata_KeyTooLong_IsRejected()
    {
        var profile = new UserProfileInfo
        {
            Metadata = new Dictionary<string, string>
            {
                [new string('k', UserProfileLimits.MetadataKey + 1)] = "value"
            }
        };

        var errors = UserProfileSafetyValidation.Validate(profile);

        errors.Should().Contain(
            x => x.Code == "profile_metadata_key_too_long");
    }

    [Fact]
    public void Metadata_ValueTooLong_IsRejected()
    {
        var profile = new UserProfileInfo
        {
            Metadata = new Dictionary<string, string>
            {
                ["key"] = new string(
                    'v',
                    UserProfileLimits.MetadataValue + 1)
            }
        };

        var errors = UserProfileSafetyValidation.Validate(profile);

        errors.Should().Contain(
            x => x.Code == "profile_metadata_value_too_long");
    }

    [Fact]
    public void Metadata_TotalUtf8SizeExceeded_IsRejected()
    {
        var metadata = Enumerable.Range(0, 5)
            .ToDictionary(
                x => $"key{x}",
                _ => new string('é', 1000));

        var profile = new UserProfileInfo
        {
            Metadata = metadata
        };

        var errors = UserProfileSafetyValidation.Validate(profile);

        errors.Should().Contain(
            x => x.Code == "profile_metadata_too_large");
    }

    [Fact]
    public void EmptyProfile_IsValid()
    {
        var errors = UserProfileSafetyValidation.Validate(
            new UserProfileInfo());

        errors.Should().BeEmpty();
    }
}
