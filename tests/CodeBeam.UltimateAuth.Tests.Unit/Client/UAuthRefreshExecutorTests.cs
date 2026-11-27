using AwesomeAssertions;
using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Abstractions;
using CodeBeam.UltimateAuth.Client.Diagnostics;
using CodeBeam.UltimateAuth.Client.Events;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit.Client;

public sealed class UAuthRefreshExecutorTests
{
    private readonly Mock<IUAuthRefreshTransport> _transport = new();
    private readonly Mock<IUAuthClientEvents> _events = new();
    private readonly InMemoryUAuthTokenStore _store = new();
    private readonly UAuthClientDiagnostics _diagnostics = new();

    private UAuthRefreshExecutor CreateSut()
    {
        _events
            .Setup(x => x.PublishAsync(It.IsAny<UAuthStateEventArgs>()))
            .Returns(Task.CompletedTask);

        return new UAuthRefreshExecutor(
            _transport.Object,
            _events.Object,
            _store,
            _diagnostics);
    }

    private static AccessToken Token(string value)
        => new()
        {
            Token = value,
            Format = TokenFormat.Jwt,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10)
        };

    [Fact]
    public async Task Manual_refresh_tracks_manual_diagnostics()
    {
        _transport
            .Setup(x => x.SendAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthTransportResult
            {
                Ok = true,
                Status = 204,
                RefreshOutcome = "touched"
            });

        var result = await CreateSut().ExecuteAsync(
            UAuthRefreshTrigger.Manual);

        result.IsSuccess.Should().BeTrue();
        result.Outcome.Should().Be(RefreshOutcome.Touched);

        _diagnostics.ManualRefreshCount.Should().Be(1);
        _diagnostics.RefreshTouchedCount.Should().Be(1);
    }

    [Fact]
    public async Task Recovery_401_preserves_existing_token()
    {
        var existing = Token("existing-token");
        _store.Replace(existing);

        _transport
            .Setup(x => x.SendAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthTransportResult
            {
                Ok = false,
                Status = 401
            });

        var result = await CreateSut().ExecuteAsync(
            UAuthRefreshTrigger.Recovery);

        result.IsSuccess.Should().BeFalse();
        result.Outcome.Should().Be(
            RefreshOutcome.ReauthRequired);

        _store.Read().Should().BeSameAs(existing);

        _events.Verify(
            x => x.PublishAsync(
                It.IsAny<UAuthStateEventArgs>()),
            Times.Never);
    }

    [Fact]
    public async Task Manual_401_clears_token_and_publishes_event()
    {
        _store.Replace(Token("old-token"));

        _transport
            .Setup(x => x.SendAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthTransportResult
            {
                Ok = false,
                Status = 401
            });

        var result = await CreateSut().ExecuteAsync(
            UAuthRefreshTrigger.Manual);

        result.Outcome.Should().Be(
            RefreshOutcome.ReauthRequired);

        _store.Read().Should().BeNull();
        _diagnostics.RefreshReauthRequiredCount
            .Should().Be(1);

        _events.Verify(
            x => x.PublishAsync(
                It.IsAny<UAuthStateEventArgs>()),
            Times.Once);
    }

    [Fact]
    public async Task Successful_refresh_replaces_access_token()
    {
        var existing = Token("old-token");
        _store.Replace(existing);

        _transport
            .Setup(x => x.SendAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthTransportResult
            {
                Ok = true,
                Status = 204,
                RefreshOutcome = "rotated",
                AccessToken = "Bearer new-token",
                AccessTokenFormat = "jwt",
                AccessTokenHeaderFormat = "bearer",
                AccessTokenExpiresAt =
                    DateTimeOffset.UtcNow
                        .AddMinutes(10)
                        .ToString("O")
            });

        var result = await CreateSut().ExecuteAsync(
            UAuthRefreshTrigger.Reactive);

        result.IsSuccess.Should().BeTrue();

        _store.Read()!.Token.Should().Be("new-token");
        _diagnostics.RefreshRotatedCount.Should().Be(1);
    }

    [Fact]
    public async Task Refresh_without_new_access_token_preserves_token()
    {
        var existing = Token("existing-token");
        _store.Replace(existing);

        _transport
            .Setup(x => x.SendAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthTransportResult
            {
                Ok = true,
                Status = 204,
                RefreshOutcome = "no-op"
            });

        var result = await CreateSut().ExecuteAsync(
            UAuthRefreshTrigger.Background);

        result.IsSuccess.Should().BeTrue();
        _store.Read().Should().BeSameAs(existing);
    }

    [Fact]
    public async Task Rate_limit_is_not_retried_immediately()
    {
        _transport
            .Setup(x => x.SendAsync(
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new UAuthTransportResult
            {
                Ok = false,
                Status = 429,
                RetryAfter = "60"
            });

        var sut = CreateSut();

        var first = await sut.ExecuteAsync(
            UAuthRefreshTrigger.Reactive);

        var second = await sut.ExecuteAsync(
            UAuthRefreshTrigger.Manual);

        first.Status.Should().Be(429);
        second.Status.Should().Be(429);
        second.RetryAfter.Should().NotBeNull();

        _transport.Verify(
            x => x.SendAsync(
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
