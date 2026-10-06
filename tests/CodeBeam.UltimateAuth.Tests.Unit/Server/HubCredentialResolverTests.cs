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

public sealed class HubCredentialResolverTests
{
    private const string AuthorizationCode = "authorization-code";
    private const string CodeVerifier = "code-verifier";

    [Fact]
    public async Task ResolveAsync_WhenArtifactDoesNotExist_ShouldReturnNull()
    {
        var fixture = CreateFixture();
        var hubSessionId = HubSessionId.New();

        fixture.Store
            .Setup(x => x.GetAsync(
                new AuthArtifactKey(hubSessionId.Value),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthArtifact?)null);

        var result = await fixture.Sut.ResolveAsync(hubSessionId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_WhenArtifactIsNotHubFlow_ShouldReturnNull()
    {
        var fixture = CreateFixture();
        var hubSessionId = HubSessionId.New();

        var artifact = new TestAuthArtifact(
            DateTimeOffset.UtcNow.AddMinutes(5));

        fixture.Store
            .Setup(x => x.GetAsync(
                new AuthArtifactKey(hubSessionId.Value),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(artifact);

        var result = await fixture.Sut.ResolveAsync(hubSessionId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_WhenFlowIsCompleted_ShouldReturnNull()
    {
        var fixture = CreateFixture();
        var artifact = CreateHubFlowArtifact();

        artifact.MarkCompleted();

        SetupArtifact(fixture, artifact);

        var result = await fixture.Sut.ResolveAsync(
            artifact.HubSessionId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_WhenAuthorizationCodeIsMissing_ShouldReturnNull()
    {
        var fixture = CreateFixture();

        var payload = new HubFlowPayload();
        payload.Set("code_verifier", CodeVerifier);

        var artifact = CreateHubFlowArtifact(payload);

        SetupArtifact(fixture, artifact);

        var result = await fixture.Sut.ResolveAsync(
            artifact.HubSessionId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_WhenAuthorizationCodeIsEmpty_ShouldReturnNull()
    {
        var fixture = CreateFixture();

        var payload = new HubFlowPayload();
        payload.Set("authorization_code", "   ");
        payload.Set("code_verifier", CodeVerifier);

        var artifact = CreateHubFlowArtifact(payload);

        SetupArtifact(fixture, artifact);

        var result = await fixture.Sut.ResolveAsync(
            artifact.HubSessionId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_WhenCodeVerifierIsMissing_ShouldReturnNull()
    {
        var fixture = CreateFixture();

        var payload = new HubFlowPayload();
        payload.Set("authorization_code", AuthorizationCode);

        var artifact = CreateHubFlowArtifact(payload);

        SetupArtifact(fixture, artifact);

        var result = await fixture.Sut.ResolveAsync(
            artifact.HubSessionId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_WhenCodeVerifierIsEmpty_ShouldReturnNull()
    {
        var fixture = CreateFixture();

        var payload = new HubFlowPayload();
        payload.Set("authorization_code", AuthorizationCode);
        payload.Set("code_verifier", "   ");

        var artifact = CreateHubFlowArtifact(payload);

        SetupArtifact(fixture, artifact);

        var result = await fixture.Sut.ResolveAsync(
            artifact.HubSessionId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_WhenCredentialsAreValid_ShouldReturnCredentials()
    {
        var fixture = CreateFixture();

        var artifact = CreateHubFlowArtifact(
            clientProfile: UAuthClientProfile.BlazorWasm);

        SetupArtifact(fixture, artifact);

        var result = await fixture.Sut.ResolveAsync(
            artifact.HubSessionId);

        result.Should().NotBeNull();

        result!.AuthorizationCode.Should().Be(AuthorizationCode);
        result.CodeVerifier.Should().Be(CodeVerifier);
        result.ClientProfile.Should().Be(UAuthClientProfile.BlazorWasm);
    }

    [Fact]
    public async Task ResolveAsync_ShouldReadArtifactUsingHubSessionId()
    {
        var fixture = CreateFixture();
        var artifact = CreateHubFlowArtifact();

        SetupArtifact(fixture, artifact);

        await fixture.Sut.ResolveAsync(
            artifact.HubSessionId);

        fixture.Store.Verify(
            x => x.GetAsync(
                It.Is<AuthArtifactKey>(key =>
                    key.Value == artifact.HubSessionId.Value),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_ShouldPropagateCancellationToken()
    {
        var fixture = CreateFixture();
        var artifact = CreateHubFlowArtifact();

        using var cts = new CancellationTokenSource();

        fixture.Store
            .Setup(x => x.GetAsync(
                It.IsAny<AuthArtifactKey>(),
                cts.Token))
            .ReturnsAsync(artifact);

        await fixture.Sut.ResolveAsync(
            artifact.HubSessionId,
            cts.Token);

        fixture.Store.Verify(
            x => x.GetAsync(
                It.Is<AuthArtifactKey>(key =>
                    key.Value == artifact.HubSessionId.Value),
                cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_WhenFlowIsExpired_ShouldReturnNull()
    {
        var fixture = CreateFixture();

        var artifact = CreateHubFlowArtifact(
            expiresAt: Now.AddMinutes(-1));

        SetupArtifact(fixture, artifact);

        var result = await fixture.Sut.ResolveAsync(
            artifact.HubSessionId);

        result.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_WhenFlowExpiresExactlyNow_ShouldReturnNull()
    {
        var fixture = CreateFixture();

        var artifact = CreateHubFlowArtifact(
            expiresAt: Now);

        SetupArtifact(fixture, artifact);

        var result = await fixture.Sut.ResolveAsync(
            artifact.HubSessionId);

        result.Should().BeNull();
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

    private static HubFlowArtifact CreateHubFlowArtifact(
        HubFlowPayload? payload = null,
        UAuthClientProfile clientProfile = UAuthClientProfile.BlazorWasm,
        DateTimeOffset? expiresAt = null)
    {
        payload ??= CreateValidPayload();

        return new HubFlowArtifact(
            HubSessionId.New(),
            HubFlowType.Login,
            clientProfile,
            TenantKey.Single,
            TestDevice.Default(),
            returnUrl: "/dashboard",
            payload,
            expiresAt ?? Now.AddMinutes(5));
    }

    private static HubFlowPayload CreateValidPayload()
    {
        var payload = new HubFlowPayload();

        payload.Set(
            "authorization_code",
            AuthorizationCode);

        payload.Set(
            "code_verifier",
            CodeVerifier);

        return payload;
    }

    private static readonly DateTimeOffset Now =
    new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    private static Fixture CreateFixture()
    {
        var store = new Mock<IAuthStore>();
        var clock = new Mock<IClock>();

        clock
            .SetupGet(x => x.UtcNow)
            .Returns(Now);

        return new Fixture(
            new HubCredentialResolver(
                store.Object,
                clock.Object),
            store,
            clock);
    }

    private sealed record Fixture(
        HubCredentialResolver Sut,
        Mock<IAuthStore> Store,
        Mock<IClock> Clock);

    private sealed class TestAuthArtifact : AuthArtifact
    {
        public TestAuthArtifact(DateTimeOffset expiresAt)
            : base(
                AuthArtifactType.Custom,
                expiresAt)
        {
        }
    }
}
