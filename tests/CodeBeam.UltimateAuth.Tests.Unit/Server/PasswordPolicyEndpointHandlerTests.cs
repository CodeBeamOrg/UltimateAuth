using AwesomeAssertions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Options;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Endpoints;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server;

public sealed class PasswordPolicyEndpointHandlerTests
{
    [Fact]
    public async Task GetAsync_WhenAllowed_ReturnsEffectivePolicyAndNoStore()
    {
        var f = new Fixture();

        f.AllowExecution();

        var result = await f.Sut.GetAsync(f.HttpContext);

        var ok = result.Should()
            .BeOfType<Ok<UAuthPasswordPolicyInfo>>()
            .Subject;

        ok.Value.Should().NotBeNull();

        var info = ok.Value!;

        info.Level.Should().Be(
            f.ExpectedPolicy.Level);
        info.MinLength.Should().Be(
            f.ExpectedPolicy.MinLength);
        info.MaxLength.Should().Be(
            f.ExpectedPolicy.MaxLength);

        f.HttpContext.Response.Headers.CacheControl
            .ToString().Should().Be("no-store");

        f.VerifyAccessContextCreated();
        f.Orchestrator.Verify(
            x => x.ExecuteAsync(
                f.AccessContext,
                It.IsAny<IAccessCommand<UAuthPasswordPolicyInfo>>(),
                f.HttpContext.RequestAborted),
            Times.Once);
    }

    [Fact]
    public async Task GetAsync_WhenAccessDenied_DoesNotExecutePolicyCommand()
    {
        var f = new Fixture();

        f.Orchestrator
            .Setup(x => x.ExecuteAsync(
                f.AccessContext,
                It.IsAny<IAccessCommand<UAuthPasswordPolicyInfo>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new UnauthorizedAccessException(
                "Access denied by policy"));

        var act = () => f.Sut.GetAsync(f.HttpContext);

        await act.Should()
            .ThrowAsync<UnauthorizedAccessException>();

        f.VerifyAccessContextCreated();

        f.HttpContext.Response.Headers.CacheControl
            .ToString().Should().BeEmpty();
    }

    [Fact]
    public async Task GetAsync_WhenRequestCancelled_PropagatesCancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var f = new Fixture(cts.Token);

        f.AllowExecution();

        var act = () => f.Sut.GetAsync(f.HttpContext);

        await act.Should()
            .ThrowAsync<OperationCanceledException>();

        f.VerifyAccessContextCreated();

        f.HttpContext.Response.Headers.CacheControl
            .ToString().Should().BeEmpty();
    }

    private sealed class Fixture
    {
        public DefaultHttpContext HttpContext { get; } = new();

        public AuthFlowContext Flow { get; } =
            AuthFlowTestFactory.New(isAuthenticated: false);

        public Mock<IAuthFlowContextAccessor> FlowAccessor { get; } = new();
        public Mock<IAccessContextFactory> ContextFactory { get; } = new();
        public Mock<IAccessOrchestrator> Orchestrator { get; } = new();

        public AccessContext AccessContext { get; }

        public UAuthPasswordPolicyInfo ExpectedPolicy { get; }

        public PasswordPolicyEndpointHandler Sut { get; }

        public Fixture(CancellationToken ct = default)
        {
            HttpContext.RequestAborted = ct;

            AccessContext = TestAccessContext.WithAction(
                UAuthActions.Credentials.GetPasswordPolicyAnonymous);

            ExpectedPolicy =
                PasswordPolicyOptionsResolver.Resolve(
                    Flow.Options.Password).ToInfo();

            FlowAccessor
                .SetupGet(x => x.Current)
                .Returns(Flow);

            ContextFactory
                .Setup(x => x.CreateAsync(
                    Flow,
                    UAuthActions.Credentials.GetPasswordPolicyAnonymous,
                    "credentials",
                    null,
                    null,
                    ct))
                .ReturnsAsync(AccessContext);

            Sut = new PasswordPolicyEndpointHandler(
                FlowAccessor.Object,
                ContextFactory.Object,
                Orchestrator.Object);
        }

        public void AllowExecution()
        {
            Orchestrator
                .Setup(x => x.ExecuteAsync(
                    AccessContext,
                    It.IsAny<IAccessCommand<UAuthPasswordPolicyInfo>>(),
                    HttpContext.RequestAborted))
                .Returns<AccessContext,
                    IAccessCommand<UAuthPasswordPolicyInfo>,
                    CancellationToken>(
                    (_, command, ct) => command.ExecuteAsync(ct));
        }

        public void VerifyAccessContextCreated()
        {
            ContextFactory.Verify(
                x => x.CreateAsync(
                    Flow,
                    UAuthActions.Credentials.GetPasswordPolicyAnonymous,
                    "credentials",
                    null,
                    null,
                    HttpContext.RequestAborted),
                Times.Once);
        }
    }
}
