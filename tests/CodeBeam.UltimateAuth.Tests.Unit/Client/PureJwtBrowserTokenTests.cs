using System.Globalization;
using System.Text.Json;
using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Abstractions;
using CodeBeam.UltimateAuth.Client.Authentication;
using CodeBeam.UltimateAuth.Client.Blazor.Infrastructure;
using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Client.Diagnostics;
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

public sealed class PureJwtBrowserTokenTests
{
    [Fact]
    public async Task Startup_restores_empty_scope_and_concurrent_refresh_shares_the_rotation()
    {
        using var fixture = new ClientScope();
        var pending = new TaskCompletionSource<UAuthTransportResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        fixture.Transport.Setup(x => x.SendFormAsync("/auth/refresh", null, default)).Returns(pending.Task);

        var restore = fixture.Coordinator.RestoreAsync();
        var concurrent = fixture.Flow.RefreshAsync();
        using var cancel = new CancellationTokenSource();
        var canceledWaiter = fixture.Flow.RefreshAsync(isAuto: false, ct: cancel.Token);
        cancel.Cancel();

        Assert.Null(fixture.Tokens.Read());
        Assert.False(fixture.State.State.IsAuthenticated);
        Assert.False(restore.IsCompleted);
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => canceledWaiter);

        var expires = DateTimeOffset.UtcNow.AddMinutes(5);
        pending.SetResult(TokenResponse("first", expires));

        var result = await concurrent;
        Assert.True(result.IsSuccess);
        Assert.Equal(RefreshOutcome.Rotated, result.Outcome);
        await restore;
        await fixture.Coordinator.RestoreAsync();
        Assert.True(fixture.State.State.IsAuthenticated);
        Assert.Equal("first", fixture.Tokens.Read()?.Value);
        Assert.Equal(expires, fixture.Tokens.Read()?.ExpiresAt);
        fixture.Transport.Verify(x => x.SendFormAsync("/auth/refresh", null, default), Times.Once);

        fixture.Transport.Setup(x => x.SendFormAsync("/auth/refresh", null, default))
            .ReturnsAsync(TokenResponse("second", expires));
        await fixture.Flow.RefreshAsync();
        Assert.Equal("second", fixture.Tokens.Read()?.Value);
        Assert.Null(new InMemoryUAuthTokenStore().Read());
    }

    [Fact]
    public async Task Logout_and_rejected_refresh_clear_credentials_and_state()
    {
        using var fixture = new ClientScope();
        fixture.Transport.Setup(x => x.SendFormAsync("/auth/refresh", null, default))
            .ReturnsAsync(TokenResponse("first", DateTimeOffset.UtcNow.AddMinutes(5)));
        fixture.Transport.Setup(x => x.SendFormAsync("/auth/logout", null, false, default))
            .ReturnsAsync(new UAuthTransportResult { Ok = true, Status = 200 });

        await fixture.Flow.RefreshAsync();
        await fixture.Flow.LogoutAsync(navigate: false);
        Assert.Null(fixture.Tokens.Read());
        Assert.False(fixture.State.State.IsAuthenticated);
        fixture.Transport.Verify(x => x.SendFormAsync("/auth/logout", null, false, default), Times.Once);

        fixture.Transport.Setup(x => x.SendFormAsync("/auth/refresh", null, default))
            .ReturnsAsync(new UAuthTransportResult { Status = 401 });
        await fixture.Coordinator.RestoreAsync();
        Assert.Null(fixture.Tokens.Read());
        Assert.False(fixture.State.State.IsAuthenticated);
    }

    private static UAuthTransportResult TokenResponse(string token, DateTimeOffset expiresAt)
        => new()
        {
            Ok = true,
            Status = 204,
            AccessToken = "Bearer " + token,
            AccessTokenExpiresAt = expiresAt.ToString("O", CultureInfo.InvariantCulture)
        };

    private sealed class ClientScope : IDisposable
    {
        public Mock<IUAuthRequestClient> Transport { get; } = new(MockBehavior.Strict);
        public IUAuthTokenStore Tokens { get; } = new InMemoryUAuthTokenStore();
        public IFlowClient Flow { get; }
        public UAuthStateManager State { get; }
        public SessionCoordinator Coordinator { get; }

        public ClientScope()
        {
            var options = Options.Create(new UAuthClientOptions { UsePureJwtBrowserTokens = true });
            var events = new UAuthClientEvents();
            var diagnostics = new UAuthClientDiagnostics();
            var clock = new TestClock();
            Flow = new UAuthFlowClient(Transport.Object, events, Mock.Of<IClientDeviceProvider>(),
                Mock.Of<IReturnUrlProvider>(), options, diagnostics, Tokens);
            var client = Mock.Of<IUAuthClient>(x => x.Flows == Flow);
            State = new UAuthStateManager(client, events, clock, Tokens, options);
            Coordinator = new SessionCoordinator(client, Mock.Of<NavigationManager>(), options, diagnostics, clock);

            var result = new AuthValidationResult
            {
                State = SessionState.Active,
                Snapshot = new AuthStateSnapshot
                {
                    Identity = new AuthIdentitySnapshot { UserKey = UserKey.FromString("user"), Tenant = TenantKey.Single },
                    Claims = ClaimsSnapshot.Empty
                }
            };
            Transport.Setup(x => x.SendFormAsync("/auth/validate", null, default))
                .ReturnsAsync(new UAuthTransportResult { Ok = true, Status = 200, Body = JsonSerializer.SerializeToElement(result) });
        }

        public void Dispose() => State.Dispose();
    }
}
