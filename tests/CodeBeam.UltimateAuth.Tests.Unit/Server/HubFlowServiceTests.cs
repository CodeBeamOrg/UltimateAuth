using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Contracts;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Server.Services;
using CodeBeam.UltimateAuth.Server.Stores;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public sealed class HubFlowServiceTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 1, 15, 12, 0, 0, TimeSpan.Zero);

    private static readonly TimeSpan FlowLifetime =
        TimeSpan.FromMinutes(10);

    private const string AuthorizationCode = "authorization-code";
    private const string CodeVerifier = "code-verifier";
    private const string ReturnUrl = "/dashboard";

    // ---------------------------------------------------------------------
    // BeginLoginAsync
    // ---------------------------------------------------------------------

    [Fact]
    public async Task BeginLoginAsync_ShouldCreateAndStoreHubFlowArtifact()
    {
        var fixture = CreateFixture();

        var request = CreateBeginRequest();

        AuthArtifactKey? capturedKey = null;
        AuthArtifact? capturedArtifact = null;

        fixture.Store
            .Setup(x => x.StoreAsync(
                It.IsAny<AuthArtifactKey>(),
                It.IsAny<AuthArtifact>(),
                It.IsAny<CancellationToken>()))
            .Callback<AuthArtifactKey, AuthArtifact, CancellationToken>(
                (key, artifact, _) =>
                {
                    capturedKey = key;
                    capturedArtifact = artifact;
                })
            .Returns(Task.CompletedTask);

        var result = await fixture.Sut.BeginLoginAsync(request);

        result.Should().NotBeNull();
        result.HubSessionId.Should().NotBeNullOrWhiteSpace();

        capturedKey.Should().NotBeNull();
        capturedArtifact.Should().BeOfType<HubFlowArtifact>();

        var flow = (HubFlowArtifact)capturedArtifact!;

        capturedKey!.Value.Should().Be(result.HubSessionId);
        flow.HubSessionId.Value.Should().Be(result.HubSessionId);

        flow.FlowType.Should().Be(HubFlowType.Login);
        flow.ClientProfile.Should().Be(request.ClientProfile);
        flow.Tenant.Should().Be(request.Tenant);
        flow.Device.Should().BeSameAs(request.Device);
        flow.ReturnUrl.Should().Be(request.ReturnUrl);

        flow.IsCompleted.Should().BeFalse();
        flow.AttemptCount.Should().Be(0);
    }

    [Fact]
    public async Task BeginLoginAsync_ShouldStoreAuthorizationCodeAndCodeVerifier()
    {
        var fixture = CreateFixture();

        HubFlowArtifact? captured = null;

        fixture.Store
            .Setup(x => x.StoreAsync(
                It.IsAny<AuthArtifactKey>(),
                It.IsAny<AuthArtifact>(),
                It.IsAny<CancellationToken>()))
            .Callback<AuthArtifactKey, AuthArtifact, CancellationToken>(
                (_, artifact, _) =>
                {
                    captured = artifact as HubFlowArtifact;
                })
            .Returns(Task.CompletedTask);

        await fixture.Sut.BeginLoginAsync(
            CreateBeginRequest());

        captured.Should().NotBeNull();

        captured!.Payload
            .GetRequired<string>("authorization_code")
            .Should().Be(AuthorizationCode);

        captured.Payload
            .GetRequired<string>("code_verifier")
            .Should().Be(CodeVerifier);
    }

    [Fact]
    public async Task BeginLoginAsync_ShouldSetExpirationFromClockAndConfiguredLifetime()
    {
        var fixture = CreateFixture();

        HubFlowArtifact? captured = null;

        fixture.Store
            .Setup(x => x.StoreAsync(
                It.IsAny<AuthArtifactKey>(),
                It.IsAny<AuthArtifact>(),
                It.IsAny<CancellationToken>()))
            .Callback<AuthArtifactKey, AuthArtifact, CancellationToken>(
                (_, artifact, _) =>
                {
                    captured = artifact as HubFlowArtifact;
                })
            .Returns(Task.CompletedTask);

        await fixture.Sut.BeginLoginAsync(
            CreateBeginRequest());

        captured.Should().NotBeNull();
        captured!.ExpiresAt.Should().Be(Now.Add(FlowLifetime));
    }

    [Fact]
    public async Task BeginLoginAsync_WhenPreviousHubSessionIdExists_ShouldConsumePreviousFlow()
    {
        var fixture = CreateFixture();

        const string previousId = "previous-hub-session";

        var request = CreateBeginRequest(
            previousHubSessionId: previousId);

        await fixture.Sut.BeginLoginAsync(request);

        fixture.Store.Verify(
            x => x.ConsumeAsync(
                It.Is<AuthArtifactKey>(key =>
                    key.Value == previousId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task BeginLoginAsync_WhenPreviousHubSessionIdIsMissing_ShouldNotConsume(
        string? previousHubSessionId)
    {
        var fixture = CreateFixture();

        var request = CreateBeginRequest(
            previousHubSessionId: previousHubSessionId);

        await fixture.Sut.BeginLoginAsync(request);

        fixture.Store.Verify(
            x => x.ConsumeAsync(
                It.IsAny<AuthArtifactKey>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task BeginLoginAsync_ShouldPropagateCancellationTokenToStore()
    {
        var fixture = CreateFixture();

        using var cts = new CancellationTokenSource();

        await fixture.Sut.BeginLoginAsync(
            CreateBeginRequest(),
            cts.Token);

        fixture.Store.Verify(
            x => x.StoreAsync(
                It.IsAny<AuthArtifactKey>(),
                It.IsAny<AuthArtifact>(),
                cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task BeginLoginAsync_WithPreviousFlow_ShouldPropagateCancellationTokenToConsume()
    {
        var fixture = CreateFixture();

        using var cts = new CancellationTokenSource();

        await fixture.Sut.BeginLoginAsync(
            CreateBeginRequest(
                previousHubSessionId: "previous-session"),
            cts.Token);

        fixture.Store.Verify(
            x => x.ConsumeAsync(
                It.Is<AuthArtifactKey>(key =>
                    key.Value == "previous-session"),
                cts.Token),
            Times.Once);
    }

    // ---------------------------------------------------------------------
    // ContinuePkceAsync
    // ---------------------------------------------------------------------

    [Fact]
    public async Task ContinuePkceAsync_ShouldReplacePkceCredentials()
    {
        var fixture = CreateFixture();

        var artifact = CreateArtifact();

        SetupArtifact(fixture, artifact);

        const string newAuthorizationCode = "new-authorization-code";
        const string newCodeVerifier = "new-code-verifier";

        await fixture.Sut.ContinuePkceAsync(
            artifact.HubSessionId.Value,
            newAuthorizationCode,
            newCodeVerifier);

        artifact.Payload
            .GetRequired<string>("authorization_code")
            .Should().Be(newAuthorizationCode);

        artifact.Payload
            .GetRequired<string>("code_verifier")
            .Should().Be(newCodeVerifier);
    }

    [Fact]
    public async Task ContinuePkceAsync_ShouldClearExistingError()
    {
        var fixture = CreateFixture();

        var artifact = CreateArtifact();

        // Use any actual HubErrorCode value from the project.
        artifact.SetError(GetTestHubErrorCode());

        artifact.Error.Should().NotBeNull();

        var attemptsBeforeContinuation = artifact.AttemptCount;

        SetupArtifact(fixture, artifact);

        await fixture.Sut.ContinuePkceAsync(
            artifact.HubSessionId.Value,
            "new-code",
            "new-verifier");

        artifact.Error.Should().BeNull();

        // ClearError() currently registers another attempt.
        artifact.AttemptCount.Should().Be(
            attemptsBeforeContinuation + 1);
    }

    [Fact]
    public async Task ContinuePkceAsync_ShouldStoreUpdatedArtifact()
    {
        var fixture = CreateFixture();

        var artifact = CreateArtifact();

        SetupArtifact(fixture, artifact);

        await fixture.Sut.ContinuePkceAsync(
            artifact.HubSessionId.Value,
            "new-code",
            "new-verifier");

        fixture.Store.Verify(
            x => x.StoreAsync(
                It.Is<AuthArtifactKey>(key =>
                    key.Value == artifact.HubSessionId.Value),
                It.Is<AuthArtifact>(stored =>
                    ReferenceEquals(stored, artifact)),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ContinuePkceAsync_WhenArtifactDoesNotExist_ShouldThrowValidationError()
    {
        var fixture = CreateFixture();

        fixture.Store
            .Setup(x => x.GetAsync(
                It.IsAny<AuthArtifactKey>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthArtifact?)null);

        var act = () => fixture.Sut.ContinuePkceAsync(
            "missing-hub-session",
            AuthorizationCode,
            CodeVerifier);

        var exception = await act.Should()
            .ThrowAsync<UAuthValidationException>();

        exception.Which.Code.Should().Be("Hub session not found.");

        fixture.Store.Verify(
            x => x.StoreAsync(
                It.IsAny<AuthArtifactKey>(),
                It.IsAny<AuthArtifact>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ContinuePkceAsync_WhenArtifactIsWrongType_ShouldThrow()
    {
        var fixture = CreateFixture();

        fixture.Store
            .Setup(x => x.GetAsync(
                It.IsAny<AuthArtifactKey>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                new TestAuthArtifact(
                    Now.AddMinutes(5)));

        var act = () => fixture.Sut.ContinuePkceAsync(
            "hub-session",
            AuthorizationCode,
            CodeVerifier);

        var exception = await act.Should()
            .ThrowAsync<UAuthValidationException>();

        exception.Which.Code.Should().Be("Hub session not found.");

        fixture.Store.Verify(
            x => x.StoreAsync(
                It.IsAny<AuthArtifactKey>(),
                It.IsAny<AuthArtifact>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    // ---------------------------------------------------------------------
    // SECURITY / LIFECYCLE INVARIANTS
    // These two are expected to expose the current implementation.
    // ---------------------------------------------------------------------

    [Fact]
    public async Task ContinuePkceAsync_WhenFlowIsExpired_ShouldRejectContinuation()
    {
        var fixture = CreateFixture();

        var artifact = CreateArtifact(
            expiresAt: Now.AddMinutes(-1));

        SetupArtifact(fixture, artifact);

        var originalCode =
            artifact.Payload.GetRequired<string>(
                "authorization_code");

        var originalVerifier =
            artifact.Payload.GetRequired<string>(
                "code_verifier");

        var act = () => fixture.Sut.ContinuePkceAsync(
            artifact.HubSessionId.Value,
            "attacker-new-code",
            "attacker-new-verifier");

        var exception = await act.Should()
            .ThrowAsync<UAuthValidationException>();

        exception.Which.Code.Should().Be("Hub session expired.");

        artifact.Payload
            .GetRequired<string>("authorization_code")
            .Should().Be(originalCode);

        artifact.Payload
            .GetRequired<string>("code_verifier")
            .Should().Be(originalVerifier);

        fixture.Store.Verify(
            x => x.StoreAsync(
                It.IsAny<AuthArtifactKey>(),
                It.IsAny<AuthArtifact>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ContinuePkceAsync_WhenFlowExpiresExactlyNow_ShouldRejectContinuation()
    {
        var fixture = CreateFixture();

        var artifact = CreateArtifact(
            expiresAt: Now);

        SetupArtifact(fixture, artifact);

        var act = () => fixture.Sut.ContinuePkceAsync(
            artifact.HubSessionId.Value,
            "new-code",
            "new-verifier");

        var exception = await act.Should()
            .ThrowAsync<UAuthValidationException>();

        exception.Which.Code.Should().Be("Hub session expired.");

        fixture.Store.Verify(
            x => x.StoreAsync(
                It.IsAny<AuthArtifactKey>(),
                It.IsAny<AuthArtifact>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ContinuePkceAsync_WhenFlowIsCompleted_ShouldRejectContinuation()
    {
        var fixture = CreateFixture();

        var artifact = CreateArtifact();

        artifact.MarkCompleted();

        SetupArtifact(fixture, artifact);

        var originalCode =
            artifact.Payload.GetRequired<string>(
                "authorization_code");

        var originalVerifier =
            artifact.Payload.GetRequired<string>(
                "code_verifier");

        var act = () => fixture.Sut.ContinuePkceAsync(
            artifact.HubSessionId.Value,
            "new-code",
            "new-verifier");

        var exception = await act.Should()
            .ThrowAsync<UAuthValidationException>();

        exception.Which.Code.Should().Be("Hub session already completed.");

        artifact.Payload
            .GetRequired<string>("authorization_code")
            .Should().Be(originalCode);

        artifact.Payload
            .GetRequired<string>("code_verifier")
            .Should().Be(originalVerifier);

        fixture.Store.Verify(
            x => x.StoreAsync(
                It.IsAny<AuthArtifactKey>(),
                It.IsAny<AuthArtifact>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ContinuePkceAsync_ShouldPropagateCancellationToken()
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

        await fixture.Sut.ContinuePkceAsync(
            artifact.HubSessionId.Value,
            "new-code",
            "new-verifier",
            cts.Token);

        fixture.Store.Verify(
            x => x.GetAsync(
                It.IsAny<AuthArtifactKey>(),
                cts.Token),
            Times.Once);

        fixture.Store.Verify(
            x => x.StoreAsync(
                It.IsAny<AuthArtifactKey>(),
                It.IsAny<AuthArtifact>(),
                cts.Token),
            Times.Once);
    }

    // ---------------------------------------------------------------------
    // ConsumeAsync
    // ---------------------------------------------------------------------

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ConsumeAsync_WhenHubSessionIdIsEmpty_ShouldDoNothing(
        string hubSessionId)
    {
        var fixture = CreateFixture();

        await fixture.Sut.ConsumeAsync(hubSessionId);

        fixture.Store.Verify(
            x => x.ConsumeAsync(
                It.IsAny<AuthArtifactKey>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ConsumeAsync_WhenHubSessionIdExists_ShouldConsumeArtifact()
    {
        var fixture = CreateFixture();

        const string hubSessionId = "hub-session";

        await fixture.Sut.ConsumeAsync(hubSessionId);

        fixture.Store.Verify(
            x => x.ConsumeAsync(
                It.Is<AuthArtifactKey>(key =>
                    key.Value == hubSessionId),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task ConsumeAsync_ShouldPropagateCancellationToken()
    {
        var fixture = CreateFixture();

        using var cts = new CancellationTokenSource();

        await fixture.Sut.ConsumeAsync(
            "hub-session",
            cts.Token);

        fixture.Store.Verify(
            x => x.ConsumeAsync(
                It.Is<AuthArtifactKey>(key =>
                    key.Value == "hub-session"),
                cts.Token),
            Times.Once);
    }

    // ---------------------------------------------------------------------
    // Fixture
    // ---------------------------------------------------------------------

    private static Fixture CreateFixture()
    {
        var store = new Mock<IAuthStore>();

        store
            .Setup(x => x.StoreAsync(
                It.IsAny<AuthArtifactKey>(),
                It.IsAny<AuthArtifact>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        store
            .Setup(x => x.ConsumeAsync(
                It.IsAny<AuthArtifactKey>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((AuthArtifact?)null);

        var clock = new Mock<IClock>();

        clock
            .SetupGet(x => x.UtcNow)
            .Returns(Now);

        var options = new UAuthServerOptions();

        options.Hub.FlowLifetime = FlowLifetime;

        var sut = new HubFlowService(
            store.Object,
            clock.Object,
            Options.Create(options));

        return new Fixture(
            sut,
            store,
            clock);
    }

    private static HubBeginRequest CreateBeginRequest(
        string? previousHubSessionId = null)
    {
        return new HubBeginRequest
        {
            AuthorizationCode = AuthorizationCode,
            CodeVerifier = CodeVerifier,
            ClientProfile = UAuthClientProfile.BlazorWasm,
            Tenant = TenantKey.Single,
            Device = TestDevice.Default(),
            ReturnUrl = ReturnUrl,
            PreviousHubSessionId = previousHubSessionId
        };
    }

    private static HubFlowArtifact CreateArtifact(
        DateTimeOffset? expiresAt = null)
    {
        var payload = new HubFlowPayload();

        payload.Set(
            "authorization_code",
            AuthorizationCode);

        payload.Set(
            "code_verifier",
            CodeVerifier);

        return new HubFlowArtifact(
            HubSessionId.New(),
            HubFlowType.Login,
            UAuthClientProfile.BlazorWasm,
            TenantKey.Single,
            TestDevice.Default(),
            ReturnUrl,
            payload,
            expiresAt ?? Now.Add(FlowLifetime));
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
        HubFlowService Sut,
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
