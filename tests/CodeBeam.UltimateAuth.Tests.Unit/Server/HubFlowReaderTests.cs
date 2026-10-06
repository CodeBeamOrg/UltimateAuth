using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Stores;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public sealed class HubFlowReaderTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    private const string ReturnUrl = "/dashboard";

    // ---------------------------------------------------------------------
    // Artifact resolution
    // ---------------------------------------------------------------------

    [Fact]
    public async Task GetStateAsync_WhenArtifactDoesNotExist_ShouldReturnNull()
    {
        var fixture = CreateFixture();
        var hubSessionId = HubSessionId.New();

        fixture.Store
            .Setup(x => x.GetAsync(
                It.Is<AuthArtifactKey>(key =>
                    key.Value == hubSessionId.Value),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthArtifact?)null);

        var result = await fixture.Sut.GetStateAsync(hubSessionId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetStateAsync_WhenArtifactIsNotHubFlow_ShouldReturnNull()
    {
        var fixture = CreateFixture();
        var hubSessionId = HubSessionId.New();

        fixture.Store
            .Setup(x => x.GetAsync(
                It.Is<AuthArtifactKey>(key =>
                    key.Value == hubSessionId.Value),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new TestAuthArtifact(
                    Now.AddMinutes(5)));

        var result = await fixture.Sut.GetStateAsync(hubSessionId);

        result.Should().BeNull();
    }

    // ---------------------------------------------------------------------
    // Active state
    // ---------------------------------------------------------------------

    [Fact]
    public async Task GetStateAsync_WhenFlowIsActive_ShouldReturnActiveState()
    {
        var fixture = CreateFixture();

        var artifact = CreateArtifact(
            expiresAt: Now.AddMinutes(5));

        SetupArtifact(fixture, artifact);

        var result = await fixture.Sut.GetStateAsync(
            artifact.HubSessionId);

        result.Should().NotBeNull();

        result!.Exists.Should().BeTrue();
        result.IsActive.Should().BeTrue();
        result.IsExpired.Should().BeFalse();
        result.IsCompleted.Should().BeFalse();
    }

    // ---------------------------------------------------------------------
    // Expiration
    // ---------------------------------------------------------------------

    [Fact]
    public async Task GetStateAsync_WhenFlowIsExpired_ShouldReturnExpiredState()
    {
        var fixture = CreateFixture();

        var artifact = CreateArtifact(
            expiresAt: Now.AddMinutes(-1));

        SetupArtifact(fixture, artifact);

        var result = await fixture.Sut.GetStateAsync(
            artifact.HubSessionId);

        result.Should().NotBeNull();

        result!.Exists.Should().BeTrue();
        result.IsActive.Should().BeFalse();
        result.IsExpired.Should().BeTrue();
        result.IsCompleted.Should().BeFalse();
    }

    [Fact]
    public async Task GetStateAsync_WhenFlowExpiresExactlyNow_ShouldReturnExpiredState()
    {
        var fixture = CreateFixture();

        var artifact = CreateArtifact(
            expiresAt: Now);

        SetupArtifact(fixture, artifact);

        var result = await fixture.Sut.GetStateAsync(
            artifact.HubSessionId);

        result.Should().NotBeNull();

        result!.Exists.Should().BeTrue();
        result.IsActive.Should().BeFalse();
        result.IsExpired.Should().BeTrue();
        result.IsCompleted.Should().BeFalse();
    }

    // ---------------------------------------------------------------------
    // Completion
    // ---------------------------------------------------------------------

    [Fact]
    public async Task GetStateAsync_WhenFlowIsCompleted_ShouldReturnCompletedState()
    {
        var fixture = CreateFixture();

        var artifact = CreateArtifact(
            expiresAt: Now.AddMinutes(5));

        artifact.MarkCompleted();

        SetupArtifact(fixture, artifact);

        var result = await fixture.Sut.GetStateAsync(
            artifact.HubSessionId);

        result.Should().NotBeNull();

        result!.Exists.Should().BeTrue();
        result.IsActive.Should().BeFalse();
        result.IsExpired.Should().BeFalse();
        result.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task GetStateAsync_WhenFlowIsExpiredAndCompleted_ShouldPreserveBothStates()
    {
        var fixture = CreateFixture();

        var artifact = CreateArtifact(
            expiresAt: Now.AddMinutes(-1));

        artifact.MarkCompleted();

        SetupArtifact(fixture, artifact);

        var result = await fixture.Sut.GetStateAsync(
            artifact.HubSessionId);

        result.Should().NotBeNull();

        result!.Exists.Should().BeTrue();
        result.IsActive.Should().BeFalse();
        result.IsExpired.Should().BeTrue();
        result.IsCompleted.Should().BeTrue();
    }

    // ---------------------------------------------------------------------
    // Mapping
    // ---------------------------------------------------------------------

    [Fact]
    public async Task GetStateAsync_ShouldMapHubFlowState()
    {
        var fixture = CreateFixture();

        var artifact = CreateArtifact(
            flowType: HubFlowType.Reauthentication,
            clientProfile: UAuthClientProfile.BlazorWasm,
            returnUrl: "/account/security");

        SetupArtifact(fixture, artifact);

        var result = await fixture.Sut.GetStateAsync(
            artifact.HubSessionId);

        result.Should().NotBeNull();

        result!.HubSessionId.Should().Be(
            artifact.HubSessionId);

        result.FlowType.Should().Be(
            HubFlowType.Reauthentication);

        result.ClientProfile.Should().Be(
            UAuthClientProfile.BlazorWasm);

        result.ReturnUrl.Should().Be(
            "/account/security");

        result.AttemptCount.Should().Be(0);

        result.Error.Should().BeNull();

        result.Exists.Should().BeTrue();
    }

    [Fact]
    public async Task GetStateAsync_ShouldMapErrorAndAttemptCount()
    {
        var fixture = CreateFixture();

        var artifact = CreateArtifact();

        var error = GetTestHubErrorCode();

        artifact.SetError(error);

        SetupArtifact(fixture, artifact);

        var result = await fixture.Sut.GetStateAsync(
            artifact.HubSessionId);

        result.Should().NotBeNull();

        result!.Error.Should().Be(error);
        result.AttemptCount.Should().Be(1);
    }

    [Fact]
    public async Task GetStateAsync_ShouldPreserveMultipleAttempts()
    {
        var fixture = CreateFixture();

        var artifact = CreateArtifact();

        var error = GetTestHubErrorCode();

        artifact.SetError(error);
        artifact.ClearError();
        artifact.SetError(error);

        artifact.AttemptCount.Should().Be(3);

        SetupArtifact(fixture, artifact);

        var result = await fixture.Sut.GetStateAsync(
            artifact.HubSessionId);

        result.Should().NotBeNull();

        result!.AttemptCount.Should().Be(3);
        result.Error.Should().Be(error);
    }

    // ---------------------------------------------------------------------
    // Store interaction
    // ---------------------------------------------------------------------

    [Fact]
    public async Task GetStateAsync_ShouldReadArtifactUsingHubSessionId()
    {
        var fixture = CreateFixture();

        var artifact = CreateArtifact();

        SetupArtifact(fixture, artifact);

        await fixture.Sut.GetStateAsync(
            artifact.HubSessionId);

        fixture.Store.Verify(
            x => x.GetAsync(
                It.Is<AuthArtifactKey>(key =>
                    key.Value == artifact.HubSessionId.Value),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetStateAsync_ShouldPropagateCancellationToken()
    {
        var fixture = CreateFixture();

        var artifact = CreateArtifact();

        using var cts = new CancellationTokenSource();

        fixture.Store
            .Setup(x => x.GetAsync(
                It.Is<AuthArtifactKey>(key =>
                    key.Value == artifact.HubSessionId.Value),
                cts.Token))
            .ReturnsAsync(artifact);

        await fixture.Sut.GetStateAsync(
            artifact.HubSessionId,
            cts.Token);

        fixture.Store.Verify(
            x => x.GetAsync(
                It.Is<AuthArtifactKey>(key =>
                    key.Value == artifact.HubSessionId.Value),
                cts.Token),
            Times.Once);
    }

    // ---------------------------------------------------------------------
    // Fixture
    // ---------------------------------------------------------------------

    private static Fixture CreateFixture()
    {
        var store = new Mock<IAuthStore>();

        var clock = new Mock<IClock>();

        clock
            .SetupGet(x => x.UtcNow)
            .Returns(Now);

        var sut = new HubFlowReader(
            store.Object,
            clock.Object);

        return new Fixture(
            sut,
            store,
            clock);
    }

    private static HubFlowArtifact CreateArtifact(
        DateTimeOffset? expiresAt = null,
        HubFlowType flowType = HubFlowType.Login,
        UAuthClientProfile clientProfile = UAuthClientProfile.BlazorWasm,
        string? returnUrl = ReturnUrl)
    {
        var payload = new HubFlowPayload();

        payload.Set(
            "authorization_code",
            "authorization-code");

        payload.Set(
            "code_verifier",
            "code-verifier");

        return new HubFlowArtifact(
            HubSessionId.New(),
            flowType,
            clientProfile,
            TenantKey.Single,
            TestDevice.Default(),
            returnUrl,
            payload,
            expiresAt ?? Now.AddMinutes(5));
    }

    private static void SetupArtifact(
        Fixture fixture,
        HubFlowArtifact artifact)
    {
        fixture.Store
            .Setup(x => x.GetAsync(
                It.Is<AuthArtifactKey>(key =>
                    key.Value == artifact.HubSessionId.Value),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(artifact);
    }

    private static HubErrorCode GetTestHubErrorCode()
    {
        return Enum.GetValues<HubErrorCode>()
            .First();
    }

    private sealed record Fixture(
        HubFlowReader Sut,
        Mock<IAuthStore> Store,
        Mock<IClock> Clock);

    private sealed class TestAuthArtifact : AuthArtifact
    {
        public TestAuthArtifact(
            DateTimeOffset expiresAt)
            : base(
                AuthArtifactType.Custom,
                expiresAt)
        {
        }
    }
}
