using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Services;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using CodeBeam.UltimateAuth.Users;
using CodeBeam.UltimateAuth.Users.Contracts;
using FluentAssertions;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public sealed class UserCreateValidatorIdentifierTests
{
    [Fact]
    public async Task ValidateAsync_WhenNoIdentifierProvided_ShouldReturnIdentifierRequired()
    {
        var fixture = CreateFixture();

        var request = new CreateUserRequest
        {
            UserName = null,
            Email = null,
            Phone = null
        };

        var result = await fixture.Sut.ValidateAsync(
            fixture.Context,
            request);

        result.IsValid.Should().BeFalse();

        result.Errors.Should().ContainSingle(x =>
            x.Code == "identifier_required");

        fixture.IdentifierValidator.Verify(
            x => x.ValidateAsync(
                It.IsAny<AccessContext>(),
                It.IsAny<UserIdentifierInfo>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        fixture.IdentifierAvailability.Verify(
            x => x.CheckAsync(
                It.IsAny<AccessContext>(),
                It.IsAny<CheckUserIdentifierAvailabilityRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(UserIdentifierType.Username)]
    [InlineData(UserIdentifierType.Email)]
    [InlineData(UserIdentifierType.Phone)]
    public async Task ValidateAsync_WhenIdentifierIsInvalid_ShouldNotCheckAvailability(
        UserIdentifierType type)
    {
        var fixture = CreateFixture();

        fixture.IdentifierValidator
            .Setup(x => x.ValidateAsync(
                fixture.Context,
                It.Is<UserIdentifierInfo>(i =>
                    i.Type == type),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                UserIdentifierValidationResult.Failed(
                    new[]
                    {
                        new UAuthValidationError(
                            "identifier_invalid")
                    }));

        var result = await fixture.Sut.ValidateAsync(
            fixture.Context,
            CreateRequest(type, "invalid-value"));

        result.IsValid.Should().BeFalse();

        result.Errors.Should().Contain(x =>
            x.Code == "identifier_invalid");

        fixture.IdentifierAvailability.Verify(
            x => x.CheckAsync(
                It.IsAny<AccessContext>(),
                It.Is<CheckUserIdentifierAvailabilityRequest>(
                    r => r.Type == type),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Theory]
    [InlineData(
        UserIdentifierType.Username,
        "username_unavailable")]
    [InlineData(
        UserIdentifierType.Email,
        "email_unavailable")]
    [InlineData(
        UserIdentifierType.Phone,
        "phone_unavailable")]
    public async Task ValidateAsync_WhenIdentifierIsUnavailable_ShouldReturnExpectedError(
        UserIdentifierType type,
        string expectedError)
    {
        var fixture = CreateFixture();

        SetupValidIdentifier(
            fixture,
            type);

        fixture.IdentifierAvailability
            .Setup(x => x.CheckAsync(
                fixture.Context,
                It.Is<CheckUserIdentifierAvailabilityRequest>(r =>
                    r.Type == type &&
                    r.Value == "value"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                UserIdentifierAvailabilityResult.Unavailable(
                    "value"));

        var result = await fixture.Sut.ValidateAsync(
            fixture.Context,
            CreateRequest(type, "value"));

        result.IsValid.Should().BeFalse();

        result.Errors.Should().ContainSingle(x =>
            x.Code == expectedError);
    }

    [Theory]
    [InlineData(UserIdentifierType.Username)]
    [InlineData(UserIdentifierType.Email)]
    [InlineData(UserIdentifierType.Phone)]
    public async Task ValidateAsync_WhenIdentifierIsValidAndAvailable_ShouldSucceed(
        UserIdentifierType type)
    {
        var fixture = CreateFixture();

        SetupValidIdentifier(
            fixture,
            type);

        fixture.IdentifierAvailability
            .Setup(x => x.CheckAsync(
                fixture.Context,
                It.Is<CheckUserIdentifierAvailabilityRequest>(r =>
                    r.Type == type &&
                    r.Value == "value"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                UserIdentifierAvailabilityResult.Available(
                    "value"));

        var result = await fixture.Sut.ValidateAsync(
            fixture.Context,
            CreateRequest(type, "value"));

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_WhenAllIdentifiersProvided_ShouldValidateAllIdentifiers()
    {
        var fixture = CreateFixture();

        fixture.IdentifierValidator
            .Setup(x => x.ValidateAsync(
                fixture.Context,
                It.IsAny<UserIdentifierInfo>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                UserIdentifierValidationResult.Success());

        fixture.IdentifierAvailability
            .Setup(x => x.CheckAsync(
                fixture.Context,
                It.IsAny<CheckUserIdentifierAvailabilityRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                UserIdentifierAvailabilityResult.Available(
                    "normalized"));

        var result = await fixture.Sut.ValidateAsync(
            fixture.Context,
            new CreateUserRequest
            {
                UserName = "john",
                Email = "john@example.com",
                Phone = "1234567890"
            });

        result.IsValid.Should().BeTrue();

        fixture.IdentifierValidator.Verify(
            x => x.ValidateAsync(
                fixture.Context,
                It.IsAny<UserIdentifierInfo>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(3));

        fixture.IdentifierAvailability.Verify(
            x => x.CheckAsync(
                fixture.Context,
                It.IsAny<CheckUserIdentifierAvailabilityRequest>(),
                It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Fact]
    public async Task ValidateAsync_WhenAllIdentifiersProvided_ShouldUseCorrectTypesAndValues()
    {
        var fixture = CreateFixture();

        fixture.IdentifierValidator
            .Setup(x => x.ValidateAsync(
                fixture.Context,
                It.IsAny<UserIdentifierInfo>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                UserIdentifierValidationResult.Success());

        fixture.IdentifierAvailability
            .Setup(x => x.CheckAsync(
                fixture.Context,
                It.IsAny<CheckUserIdentifierAvailabilityRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                UserIdentifierAvailabilityResult.Available(
                    "normalized"));

        await fixture.Sut.ValidateAsync(
            fixture.Context,
            new CreateUserRequest
            {
                UserName = "john",
                Email = "john@example.com",
                Phone = "1234567890"
            });

        fixture.IdentifierValidator.Verify(
            x => x.ValidateAsync(
                fixture.Context,
                It.Is<UserIdentifierInfo>(i =>
                    i.Type == UserIdentifierType.Username &&
                    i.Value == "john"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        fixture.IdentifierValidator.Verify(
            x => x.ValidateAsync(
                fixture.Context,
                It.Is<UserIdentifierInfo>(i =>
                    i.Type == UserIdentifierType.Email &&
                    i.Value == "john@example.com"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        fixture.IdentifierValidator.Verify(
            x => x.ValidateAsync(
                fixture.Context,
                It.Is<UserIdentifierInfo>(i =>
                    i.Type == UserIdentifierType.Phone &&
                    i.Value == "1234567890"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        fixture.IdentifierAvailability.Verify(
            x => x.CheckAsync(
                fixture.Context,
                It.Is<CheckUserIdentifierAvailabilityRequest>(r =>
                    r.Type == UserIdentifierType.Username &&
                    r.Value == "john"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        fixture.IdentifierAvailability.Verify(
            x => x.CheckAsync(
                fixture.Context,
                It.Is<CheckUserIdentifierAvailabilityRequest>(r =>
                    r.Type == UserIdentifierType.Email &&
                    r.Value == "john@example.com"),
                It.IsAny<CancellationToken>()),
            Times.Once);

        fixture.IdentifierAvailability.Verify(
            x => x.CheckAsync(
                fixture.Context,
                It.Is<CheckUserIdentifierAvailabilityRequest>(r =>
                    r.Type == UserIdentifierType.Phone &&
                    r.Value == "1234567890"),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateAsync_WhenOneIdentifierIsInvalid_ShouldContinueValidatingOtherIdentifiers()
    {
        var fixture = CreateFixture();

        fixture.IdentifierValidator
            .Setup(x => x.ValidateAsync(
                fixture.Context,
                It.Is<UserIdentifierInfo>(i =>
                    i.Type == UserIdentifierType.Username),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                UserIdentifierValidationResult.Failed(
                    new[]
                    {
                        new UAuthValidationError(
                            "username_invalid")
                    }));

        fixture.IdentifierValidator
            .Setup(x => x.ValidateAsync(
                fixture.Context,
                It.Is<UserIdentifierInfo>(i =>
                    i.Type == UserIdentifierType.Email),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                UserIdentifierValidationResult.Success());

        fixture.IdentifierAvailability
            .Setup(x => x.CheckAsync(
                fixture.Context,
                It.Is<CheckUserIdentifierAvailabilityRequest>(r =>
                    r.Type == UserIdentifierType.Email),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                UserIdentifierAvailabilityResult.Available(
                    "john@example.com"));

        var result = await fixture.Sut.ValidateAsync(
            fixture.Context,
            new CreateUserRequest
            {
                UserName = "invalid",
                Email = "john@example.com"
            });

        result.IsValid.Should().BeFalse();

        result.Errors.Should().Contain(x =>
            x.Code == "username_invalid");

        fixture.IdentifierAvailability.Verify(
            x => x.CheckAsync(
                fixture.Context,
                It.Is<CheckUserIdentifierAvailabilityRequest>(r =>
                    r.Type == UserIdentifierType.Username),
                It.IsAny<CancellationToken>()),
            Times.Never);

        fixture.IdentifierAvailability.Verify(
            x => x.CheckAsync(
                fixture.Context,
                It.Is<CheckUserIdentifierAvailabilityRequest>(r =>
                    r.Type == UserIdentifierType.Email),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ValidateAsync_ShouldPropagateCancellationTokenToIdentifierServices()
    {
        var fixture = CreateFixture();

        using var cts =
            new CancellationTokenSource();

        fixture.IdentifierValidator
            .Setup(x => x.ValidateAsync(
                fixture.Context,
                It.IsAny<UserIdentifierInfo>(),
                cts.Token))
            .ReturnsAsync(
                UserIdentifierValidationResult.Success());

        fixture.IdentifierAvailability
            .Setup(x => x.CheckAsync(
                fixture.Context,
                It.IsAny<CheckUserIdentifierAvailabilityRequest>(),
                cts.Token))
            .ReturnsAsync(
                UserIdentifierAvailabilityResult.Available(
                    "john"));

        await fixture.Sut.ValidateAsync(
            fixture.Context,
            new CreateUserRequest
            {
                UserName = "john"
            },
            cts.Token);

        fixture.IdentifierValidator.Verify(
            x => x.ValidateAsync(
                fixture.Context,
                It.IsAny<UserIdentifierInfo>(),
                cts.Token),
            Times.Once);

        fixture.IdentifierAvailability.Verify(
            x => x.CheckAsync(
                fixture.Context,
                It.IsAny<CheckUserIdentifierAvailabilityRequest>(),
                cts.Token),
            Times.Once);
    }

    private static void SetupValidIdentifier(
        Fixture fixture,
        UserIdentifierType type)
    {
        fixture.IdentifierValidator
            .Setup(x => x.ValidateAsync(
                fixture.Context,
                It.Is<UserIdentifierInfo>(i =>
                    i.Type == type),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                UserIdentifierValidationResult.Success());
    }

    private static CreateUserRequest CreateRequest(
        UserIdentifierType type,
        string value)
    {
        return type switch
        {
            UserIdentifierType.Username =>
                new CreateUserRequest
                {
                    UserName = value
                },

            UserIdentifierType.Email =>
                new CreateUserRequest
                {
                    Email = value
                },

            UserIdentifierType.Phone =>
                new CreateUserRequest
                {
                    Phone = value
                },

            _ => throw new ArgumentOutOfRangeException(
                nameof(type))
        };
    }

    private static Fixture CreateFixture()
    {
        var identifierValidator =
            new Mock<IUserIdentifierValidator>();

        var identifierAvailability =
            new Mock<IUserIdentifierAvailabilityService>();

        var profileValidator =
            new Mock<IUserProfileValidator>();

        profileValidator
            .Setup(x => x.ValidateAsync(
                It.IsAny<AccessContext>(),
                It.IsAny<UserProfileInfo>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                UserProfileValidationResult.Success());

        var context =
            TestAccessContext.WithAction("users.create");

        var sut =
            new UserCreateValidator(
                identifierValidator.Object,
                identifierAvailability.Object,
                profileValidator.Object);

        return new Fixture(
            sut,
            context,
            identifierValidator,
            identifierAvailability,
            profileValidator);
    }

    private sealed record Fixture(
        UserCreateValidator Sut,
        AccessContext Context,
        Mock<IUserIdentifierValidator> IdentifierValidator,
        Mock<IUserIdentifierAvailabilityService> IdentifierAvailability,
        Mock<IUserProfileValidator> ProfileValidator);
}