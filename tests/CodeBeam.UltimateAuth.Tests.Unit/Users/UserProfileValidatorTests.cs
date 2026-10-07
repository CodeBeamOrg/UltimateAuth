using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Users;
using CodeBeam.UltimateAuth.Users.Contracts;
using FluentAssertions;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public sealed class UserProfileValidatorTests
{
    [Fact]
    public async Task ValidateAsync_CurrentImplementation_ShouldSucceed()
    {
        var sut = new UserProfileValidator();

        var result = await sut.ValidateAsync(
            null!,
            new UserProfileInfo());

        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task ValidateAsync_WhenCancellationRequested_ShouldThrow()
    {
        var sut = new UserProfileValidator();

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => sut.ValidateAsync(
            null!,
            new UserProfileInfo(),
            cts.Token);

        await act.Should()
            .ThrowAsync<OperationCanceledException>();
    }
}
