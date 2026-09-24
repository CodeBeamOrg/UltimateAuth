using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Authorization.Reference;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public sealed class AuthorizationServiceTests
{
    [Fact]
    public async Task AuthorizeAsync_WhenAccessOrchestratorAllows_ReturnsAllowed()
    {
        var f = new Fixture();
        var context = TestAccessContext.WithAction("orders.read");

        f.AccessOrchestrator
            .Setup(x => x.ExecuteAsync(
                context,
                It.IsAny<AccessCommand<AuthorizationResult>>(),
                It.IsAny<CancellationToken>()))
            .Returns<AccessContext, AccessCommand<AuthorizationResult>, CancellationToken>(
                async (_, command, ct) => await command.ExecuteAsync(ct));

        var result = await f.Sut.AuthorizeAsync(context);

        result.IsAllowed.Should().BeTrue();
        result.RequiresReauthentication.Should().BeFalse();
        result.DenyReason.Should().BeNull();

        f.AccessOrchestrator.Verify(x => x.ExecuteAsync(
            context,
            It.IsAny<AccessCommand<AuthorizationResult>>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AuthorizeAsync_WhenAccessOrchestratorDenies_ReturnsDenied()
    {
        var f = new Fixture();
        var context = TestAccessContext.WithAction("orders.delete");

        var exception = new UAuthAuthorizationException(
            "access_denied");

        f.AccessOrchestrator
            .Setup(x => x.ExecuteAsync(
                context,
                It.IsAny<AccessCommand<AuthorizationResult>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(exception);

        var result = await f.Sut.AuthorizeAsync(context);

        result.IsAllowed.Should().BeFalse();
        result.RequiresReauthentication.Should().BeFalse();

        // This deliberately tests CURRENT service behavior.
        result.DenyReason.Should().Be(exception.Message);
    }

    [Fact]
    public async Task AuthorizeAsync_WhenUnexpectedExceptionOccurs_DoesNotSwallowException()
    {
        var f = new Fixture();
        var context = TestAccessContext.WithAction("orders.read");

        var expected = new InvalidOperationException(
            "store unavailable");

        f.AccessOrchestrator
            .Setup(x => x.ExecuteAsync(
                context,
                It.IsAny<AccessCommand<AuthorizationResult>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(expected);

        var act = () => f.Sut.AuthorizeAsync(context);

        var exception = await act.Should()
            .ThrowAsync<InvalidOperationException>();

        exception.Which.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task AuthorizeAsync_WhenAlreadyCancelled_ThrowsBeforeAccessExecution()
    {
        var f = new Fixture();
        var context = TestAccessContext.WithAction("orders.read");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => f.Sut.AuthorizeAsync(
            context,
            cts.Token);

        await act.Should()
            .ThrowAsync<OperationCanceledException>();

        f.AccessOrchestrator.VerifyNoOtherCalls();
    }

    private sealed class Fixture
    {
        public Mock<IAccessOrchestrator> AccessOrchestrator { get; }
            = new(MockBehavior.Strict);

        public AuthorizationService Sut { get; }

        public Fixture()
        {
            Sut = new AuthorizationService(
                AccessOrchestrator.Object);
        }
    }
}
