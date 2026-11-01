using System.Net;
using System.Text.Json;
using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Authentication;
using CodeBeam.UltimateAuth.Client.Blazor;
using CodeBeam.UltimateAuth.Client.Blazor.Infrastructure;
using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Client.Diagnostics;
using CodeBeam.UltimateAuth.Client.Errors;
using CodeBeam.UltimateAuth.Client.Events;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Client.Services;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Options;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public sealed class ClientRateLimitingTests : UAuthClientTestBase
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", null)]
    [InlineData("invalid", null)]
    [InlineData("-1", null)]
    [InlineData("1.5", null)]
    [InlineData("60", 60)]
    [InlineData("0", 0)]
    [InlineData(" 120 ", 120)]
    [InlineData("9999999999999999999999999", null)]
    public void RetryAfterHandlesMissingInvalidAndDeltaSeconds(string? header, int? expected)
    {
        Assert.Equal(expected is null ? null : TimeSpan.FromSeconds(expected.Value), UAuthRateLimit.ParseRetryAfter(header, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void RetryAfterParsesHttpDateAndPastDate()
    {
        var now = new DateTimeOffset(2026, 11, 1, 12, 0, 0, TimeSpan.Zero);
        Assert.Equal(TimeSpan.FromMinutes(2), UAuthRateLimit.ParseRetryAfter(now.AddMinutes(2).ToString("r"), now));
        Assert.Equal(TimeSpan.Zero, UAuthRateLimit.ParseRetryAfter(now.AddMinutes(-1).ToString("r"), now));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("90")]
    public async Task ValidationThrottlePreservesStateAndBlazorPrincipal_ThenRevocationStillClears(string? retry)
    {
        var clock = new TestClock();
        var snapshot = new AuthStateSnapshot
        {
            Identity = new AuthIdentitySnapshot
            { UserKey = UserKey.FromString("rate-user"), Tenant = TenantKey.Single },
            Claims = ClaimsSnapshot.Empty
        };
        Request.SetupSequence(x => x.SendFormAsync("/auth/validate", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthTransportResult { Status = 200, Ok = true, Body = JsonSerializer.SerializeToElement(new AuthValidationResult { State = SessionState.Active, Snapshot = snapshot }) })
            .ReturnsAsync(new UAuthTransportResult { Status = 429, RetryAfter = retry })
            .ReturnsAsync(new UAuthTransportResult { Status = 200, Ok = true, Body = JsonSerializer.SerializeToElement(new AuthValidationResult { State = SessionState.Revoked }) });
        using var manager = new UAuthStateManager(CreateClient(flows: CreateFlowClient()), Events.Object, clock);
        var provider = new UAuthAuthenticationStateProvider(manager);
        await manager.EnsureAsync();
        var lastValidated = manager.State.LastValidatedAt;
        await manager.EnsureAsync(force: true);
        await manager.EnsureAsync(force: true);
        Assert.True(manager.State.IsAuthenticated);
        Assert.True((await provider.GetAuthenticationStateAsync()).User.Identity!.IsAuthenticated);
        Assert.Equal(lastValidated, manager.State.LastValidatedAt);
        Request.Verify(x => x.SendFormAsync("/auth/validate", null, It.IsAny<CancellationToken>()), Times.Exactly(2));
        clock.Advance(TimeSpan.FromMinutes(2));
        await manager.EnsureAsync(force: true);
        Assert.False(manager.State.IsAuthenticated);
        Assert.False((await provider.GetAuthenticationStateAsync()).User.Identity!.IsAuthenticated);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("60")]
    public async Task RefreshIsThrottledWithoutReauthOrImmediateRepeatedRequest(string? retry)
    {
        Request.Setup(x => x.SendFormAsync("/auth/refresh", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthTransportResult { Status = 429, RetryAfter = retry, RefreshOutcome = "reauth-required" });
        var flow = CreateFlowClient();
        var result = await flow.RefreshAsync();
        Assert.False(result.IsSuccess);
        Assert.Equal(429, result.Status);
        Assert.Equal("rate_limited", result.ErrorCode);
        Assert.Equal(RefreshOutcome.None, result.Outcome);
        Assert.Equal(retry is null ? null : TimeSpan.FromSeconds(60), result.RetryAfter);
        await flow.RefreshAsync(isAuto: true);
        Request.Verify(x => x.SendFormAsync("/auth/refresh", null, It.IsAny<CancellationToken>()), Times.Once);
        Events.Verify(x => x.PublishAsync(It.IsAny<UAuthStateEventArgsEmpty>()), Times.Never);
    }

    [Fact]
    public async Task GenuineRefresh401StillRequiresReauth()
    {
        Request.Setup(x => x.SendFormAsync("/auth/refresh", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthTransportResult { Status = 401 });
        var result = await CreateFlowClient().RefreshAsync();
        Assert.Equal(401, result.Status);
        Assert.Equal(RefreshOutcome.ReauthRequired, result.Outcome);
    }

    [Theory]
    [InlineData(UAuthSubmitMode.TryOnly)]
    [InlineData(UAuthSubmitMode.TryAndCommit)]
    public async Task LoginThrottleHasDistinctResultAndNeverCommits(UAuthSubmitMode mode)
    {
        Request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthTransportResult { Status = 429, RetryAfter = "60" });
        Request.Setup(x => x.TryAndCommitAsync<TryLoginResult>(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(UAuthRateLimit.Exception("60"));
        IUAuthTryResult result = await CreateFlowClient().TryLoginAsync(new LoginRequest(), mode);
        Assert.False(result.IsSuccess);
        Assert.Null(result.Reason);
        Assert.Equal(429, result.Status);
        Assert.Equal("rate_limited", result.ErrorCode);
        Assert.Equal(TimeSpan.FromMinutes(1), result.RetryAfter);
        Request.Verify(x => x.NavigateAsync(It.IsAny<string>(), It.IsAny<IDictionary<string, string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LogoutThrottleIsNotSuccessAndDoesNotPublishLogout()
    {
        Request.Setup(x => x.SendFormAsync("/auth/logout", null, true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthTransportResult { Status = 429 });
        var error = await Assert.ThrowsAsync<UAuthTransportException>(() => CreateFlowClient().LogoutAsync());
        Assert.Equal(HttpStatusCode.TooManyRequests, error.StatusCode);
        Assert.Equal("rate_limited", error.Code);
        Events.Verify(x => x.PublishAsync(It.IsAny<UAuthStateEventArgsEmpty>()), Times.Never);
    }

    [Fact]
    public async Task AuthorizationAndRecoveryThrottleAreResults_NotForbiddenOrRetry()
    {
        Request.Setup(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthTransportResult { Status = 429, RetryAfter = "30" });
        var result = await CreateClient().Authorization.CheckAsync(new CodeBeam.UltimateAuth.Authorization.Contracts.AuthorizationCheckRequest { Action = "read", Resource = "example" });
        Assert.Equal(429, result.Status);
        Assert.True(result.Http.IsTooManyRequests);
        Assert.False(result.Http.IsForbidden);
        Assert.Equal("rate_limited", result.Problem!.Detail);
        Assert.Equal(TimeSpan.FromSeconds(30), result.Problem.RetryAfter);
        Assert.Null(result.Value);
        var recovery = await CreateClient().Credentials.CompleteResetAsync(new CodeBeam.UltimateAuth.Credentials.Contracts.CompleteResetCredentialRequest
        { Identifier = "user", CredentialType = CredentialType.Password, ResetToken = "proof", NewSecret = "changed-password" });
        Assert.Equal(429, recovery.Status);
        Assert.Equal("rate_limited", recovery.Problem!.Detail);
        Events.Verify(x => x.PublishAsync(It.IsAny<UAuthStateEventArgsEmpty>()), Times.Never);
        Request.Verify(x => x.SendJsonAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(120)]
    public async Task AutomaticRefreshDefersThrottle_ThenHonorsReal401(int? seconds)
    {
        var clock = new TestClock();
        var flows = new Mock<IFlowClient>();
        flows.SetupSequence(x => x.RefreshAsync(true))
            .ReturnsAsync(new RefreshResult { Status = 429, ErrorCode = "rate_limited", RetryAfter = seconds is null ? null : TimeSpan.FromSeconds(seconds.Value) })
            .ReturnsAsync(new RefreshResult { Status = 401, Outcome = RefreshOutcome.ReauthRequired });
        var diagnostics = new UAuthClientDiagnostics();
        await using var coordinator = new SessionCoordinator(CreateClient(flows: flows.Object), Mock.Of<NavigationManager>(),
            Options.Create(new UAuthClientOptions { Reauth = new() { Behavior = ReauthBehavior.RaiseEvent } }), diagnostics, clock);
        await coordinator.TickAsync();
        await coordinator.TickAsync();
        Assert.False(diagnostics.IsTerminated);
        flows.Verify(x => x.RefreshAsync(true), Times.Once);
        clock.Advance(TimeSpan.FromMinutes(3));
        await coordinator.TickAsync();
        Assert.True(diagnostics.IsTerminated);
        flows.Verify(x => x.RefreshAsync(true), Times.Exactly(2));
    }
}
