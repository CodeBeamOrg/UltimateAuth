using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Server.ConcurrencyLimiting;
using CodeBeam.UltimateAuth.Server.Flows;
using CodeBeam.UltimateAuth.Server.Options;
using Microsoft.Extensions.Options;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server;

public sealed class LoginPasswordVerificationTests
{
    [Fact]
    public async Task DummyVerification_ShouldCreateHashOnlyOnce()
    {
        var state = new LoginPasswordVerificationState();

        using var limiter = new UAuthPasswordHashLimiter(
            Options.Create(new UAuthServerOptions()));

        var hash = PasswordHash.Create("test", "dummy-hash");

        var hasher = new Mock<IUAuthPasswordHasher>();

        hasher.Setup(x => x.Hash(It.IsAny<string>()))
            .Returns(hash);

        hasher.Setup(x => x.Verify(
                hash, It.IsAny<string>()))
            .Returns(false);

        var sut = new LoginPasswordVerification(
            hasher.Object,
            state,
            limiter);

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
        var state = new LoginPasswordVerificationState();

        using var limiter = new UAuthPasswordHashLimiter(
            Options.Create(new UAuthServerOptions()));

        var hash = PasswordHash.Create("test", "dummy-hash");

        var hasher = new Mock<IUAuthPasswordHasher>();

        hasher.Setup(x => x.Hash(It.IsAny<string>()))
            .Returns(hash);

        hasher.Setup(x => x.Verify(
                hash, It.IsAny<string>()))
            .Returns(true);

        var sut = new LoginPasswordVerification(
            hasher.Object,
            state,
            limiter);

        await sut.VerifyDummyAsync(
            "any-password",
            TestContext.Current.CancellationToken);

        hasher.Verify(
            x => x.Verify(hash, "any-password"),
            Times.Once);
    }
}
