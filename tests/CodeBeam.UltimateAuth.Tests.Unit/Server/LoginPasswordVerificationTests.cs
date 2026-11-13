using AwesomeAssertions;
using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Credentials;
using CodeBeam.UltimateAuth.Server.Flows;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server;

public sealed class LoginPasswordVerificationTests
{
    [Fact]
    public async Task DummyVerification_ShouldCreateHashOnlyOnce()
    {
        using var state = new LoginPasswordVerificationState();

        var hash = PasswordHash.Create("test", "dummy-hash");
        var hasher = new Mock<IUAuthPasswordHasher>();

        hasher
            .Setup(x => x.Hash(It.IsAny<string>()))
            .Returns(hash);

        hasher
            .Setup(x => x.Verify(hash, It.IsAny<string>()))
            .Returns(false);

        var sut = new LoginPasswordVerification(
            hasher.Object, state);

        await sut.VerifyDummyAsync(
            "first-password",
            TestContext.Current.CancellationToken);

        await sut.VerifyDummyAsync(
            "second-password",
            TestContext.Current.CancellationToken);

        hasher.Verify(
            x => x.Hash(It.IsAny<string>()),
            Times.Once);

        hasher.Verify(
            x => x.Verify(hash, "first-password"),
            Times.Once);

        hasher.Verify(
            x => x.Verify(hash, "second-password"),
            Times.Once);
    }

    [Fact]
    public async Task DummyVerification_ShouldIgnoreSuccessfulHashMatch()
    {
        using var state = new LoginPasswordVerificationState();

        var hash = PasswordHash.Create("test", "dummy-hash");
        var hasher = new Mock<IUAuthPasswordHasher>();

        hasher
            .Setup(x => x.Hash(It.IsAny<string>()))
            .Returns(hash);

        hasher
            .Setup(x => x.Verify(hash, It.IsAny<string>()))
            .Returns(true);

        var sut = new LoginPasswordVerification(
            hasher.Object, state);

        // There is deliberately no successful login result
        // or bool return value.
        await sut.VerifyDummyAsync(
            "any-password",
            TestContext.Current.CancellationToken);

        hasher.Verify(
            x => x.Verify(hash, "any-password"),
            Times.Once);
    }

    [Fact]
    public async Task ConcurrentVerifications_ShouldRejectRequestsBeyondCapacity()
    {
        using var state = new LoginPasswordVerificationState();

        var hasher = new Mock<IUAuthPasswordHasher>();
        var provider = new Mock<ICredentialProvider>();
        var credential = new Mock<ICredential>();

        var pending = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        provider
            .Setup(x => x.ValidateAsync(
                It.IsAny<ICredential>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(pending.Task);

        var sut = new LoginPasswordVerification(
            hasher.Object, state);

        var first = sut.VerifyCredentialAsync(
            provider.Object,
            credential.Object,
            "password-1",
            TestContext.Current.CancellationToken);

        var second = sut.VerifyCredentialAsync(
            provider.Object,
            credential.Object,
            "password-2",
            TestContext.Current.CancellationToken);

        try
        {
            Func<Task> third = () => sut.VerifyCredentialAsync(
                provider.Object,
                credential.Object,
                "password-3",
                TestContext.Current.CancellationToken);

            await third.Should()
                .ThrowAsync<PasswordVerificationCapacityException>();

            provider.Verify(
                x => x.ValidateAsync(
                    It.IsAny<ICredential>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }
        finally
        {
            pending.TrySetResult(false);
            await Task.WhenAll(first, second);
        }
    }

    [Fact]
    public async Task FailedVerification_ShouldReleaseConcurrencyPermit()
    {
        using var state = new LoginPasswordVerificationState();

        var hasher = new Mock<IUAuthPasswordHasher>();
        var provider = new Mock<ICredentialProvider>();
        var credential = new Mock<ICredential>();

        provider
            .Setup(x => x.ValidateAsync(
                It.IsAny<ICredential>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("test failure"));

        var sut = new LoginPasswordVerification(
            hasher.Object, state);

        for (var i = 0; i < 3; i++)
        {
            Func<Task> act = () => sut.VerifyCredentialAsync(
                provider.Object,
                credential.Object,
                "password",
                TestContext.Current.CancellationToken);

            await act.Should()
                .ThrowAsync<InvalidOperationException>();
        }

        state.Semaphore.CurrentCount.Should()
            .Be(LoginPasswordVerificationState.MaxConcurrentVerifications);
    }

    [Fact]
    public async Task DummyVerification_ShouldRespectSharedCapacity()
    {
        using var state = new LoginPasswordVerificationState();

        var hasher = new Mock<IUAuthPasswordHasher>();
        var provider = new Mock<ICredentialProvider>();
        var credential = new Mock<ICredential>();

        var pending = new TaskCompletionSource<bool>(
            TaskCreationOptions.RunContinuationsAsynchronously);

        provider
            .Setup(x => x.ValidateAsync(
                It.IsAny<ICredential>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Returns(pending.Task);

        var sut = new LoginPasswordVerification(hasher.Object, state);

        var first = sut.VerifyCredentialAsync(
            provider.Object, credential.Object, "first",
            TestContext.Current.CancellationToken);

        var second = sut.VerifyCredentialAsync(
            provider.Object, credential.Object, "second",
            TestContext.Current.CancellationToken);

        try
        {
            Func<Task> dummy = () => sut.VerifyDummyAsync(
                "unknown-user-password",
                TestContext.Current.CancellationToken);

            await dummy.Should()
                .ThrowAsync<PasswordVerificationCapacityException>();

            hasher.Verify(
                x => x.Hash(It.IsAny<string>()),
                Times.Never);

            hasher.Verify(
                x => x.Verify(
                    It.IsAny<PasswordHash>(),
                    It.IsAny<string>()),
                Times.Never);
        }
        finally
        {
            pending.TrySetResult(false);
            await Task.WhenAll(first, second);
        }
    }
}
