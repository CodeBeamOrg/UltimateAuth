using System.Net;
using CodeBeam.UltimateAuth.Client.Abstractions;
using CodeBeam.UltimateAuth.Client.Diagnostics;
using CodeBeam.UltimateAuth.Client.Errors;
using CodeBeam.UltimateAuth.Client.Events;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Client.Infrastructure;

internal sealed class UAuthRefreshExecutor : IUAuthRefreshExecutor
{
    private readonly IUAuthRefreshTransport _transport;
    private readonly IUAuthClientEvents _events;
    private readonly IUAuthTokenStore _tokenStore;
    private readonly UAuthClientDiagnostics _diagnostics;

    private DateTimeOffset _refreshNotBefore;
    private RefreshResult? _refreshThrottle;

    public UAuthRefreshExecutor(
        IUAuthRefreshTransport transport,
        IUAuthClientEvents events,
        IUAuthTokenStore tokenStore,
        UAuthClientDiagnostics diagnostics)
    {
        _transport = transport;
        _events = events;
        _tokenStore = tokenStore;
        _diagnostics = diagnostics;
    }

    public async Task<RefreshResult> ExecuteAsync(UAuthRefreshTrigger trigger)
    {
        if (trigger == UAuthRefreshTrigger.Manual)
            _diagnostics.MarkManualRefresh();

        var now = DateTimeOffset.UtcNow;

        if (_refreshThrottle is not null && now < _refreshNotBefore)
        {
            return _refreshThrottle with
            {
                RetryAfter = _refreshNotBefore - now
            };
        }

        var expected = _tokenStore.Capture();

        // Raw transport: never enters request recovery.
        var raw = await _transport.SendAsync();

        ArgumentNullException.ThrowIfNull(raw);

        if (raw.Status == 429)
        {
            _refreshThrottle = new RefreshResult
            {
                Status = 429,
                IsSuccess = false,
                Outcome = RefreshOutcome.None,
                ErrorCode = "rate_limited",
                RetryAfter = UAuthRateLimit.ParseRetryAfter(raw.RetryAfter, DateTimeOffset.UtcNow)
            };

            _refreshNotBefore = UAuthRateLimit.NotBefore(DateTimeOffset.UtcNow, _refreshThrottle.RetryAfter);

            return _refreshThrottle;
        }

        _refreshThrottle = null;
        _refreshNotBefore = default;

        if (raw.Status == 401)
        {
            var current = _tokenStore.Capture();

            if (current.Epoch != expected.Epoch)
            {
                return new RefreshResult
                {
                    IsSuccess = false,
                    Status = 401,
                    Outcome = RefreshOutcome.None,
                    ErrorCode = "stale_refresh"
                };
            }

            if (trigger != UAuthRefreshTrigger.Recovery)
            {
                // Do not erase a token changed by another operation.
                if (_tokenStore.TryClear(expected))
                {
                    _diagnostics.MarkRefreshReauthRequired();

                    await _events.PublishAsync(
                        new UAuthStateEventArgsEmpty(UAuthStateEvent.ReauthenticationRequired, UAuthStateEventHandlingMode.Patch));
                }
            }

            return new RefreshResult
            {
                IsSuccess = false,
                Status = 401,
                Outcome = RefreshOutcome.ReauthRequired
            };
        }

        EnsureValidHttpStatus(raw);

        var outcome = RefreshOutcomeParser.Parse(raw.RefreshOutcome);

        var isSuccess = raw.Ok && raw.Status is >= 200 and < 300;

        if (isSuccess)
        {
            var accessToken = UAuthLoginCompletion.ReadAccessToken(raw);

            if (accessToken is not null)
            {
                if (accessToken.IsExpired(DateTimeOffset.UtcNow))
                {
                    _tokenStore.TryClear(expected);
                    throw new UAuthProtocolException("Server returned an expired access token.");
                }

                if (!_tokenStore.TryRotate(expected, accessToken))
                {
                    return new RefreshResult
                    {
                        IsSuccess = false,
                        Status = raw.Status,
                        Outcome = RefreshOutcome.None,
                        ErrorCode = "stale_refresh"
                    };
                }
            }
            else if (_tokenStore.Capture().Epoch != expected.Epoch)
            {
                return new RefreshResult
                {
                    IsSuccess = false,
                    Status = raw.Status,
                    Outcome = RefreshOutcome.None,
                    ErrorCode = "stale_refresh"
                };
            }

            RecordOutcome(outcome);
        }

        return new RefreshResult
        {
            IsSuccess = isSuccess,
            Status = raw.Status,
            Outcome = outcome
        };
    }

    private void RecordOutcome(RefreshOutcome outcome)
    {
        switch (outcome)
        {
            case RefreshOutcome.NoOp:
                _diagnostics.MarkRefreshNoOp();
                break;

            case RefreshOutcome.Touched:
                _diagnostics.MarkRefreshTouched();
                break;

            case RefreshOutcome.Rotated:
                _diagnostics.MarkRefreshRotated();
                break;

            case RefreshOutcome.ReauthRequired:
                _diagnostics.MarkRefreshReauthRequired();
                break;

            case RefreshOutcome.Success:
                _diagnostics.MarkRefreshSuccess();
                break;
        }
    }

    private static void EnsureValidHttpStatus(
        UAuthTransportResult raw)
    {
        if (raw.Status == 0)
        {
            throw new UAuthTransportException("Network error.");
        }

        if (raw.Status is >= 500 and <= 599)
        {
            throw new UAuthTransportException($"Server error {raw.Status}.", (HttpStatusCode)raw.Status);
        }

        if (raw.Status < 200 ||
            raw.Status is >= 300 and < 400 ||
            raw.Status >= 600)
        {
            throw new UAuthProtocolException($"Unexpected HTTP status code: {raw.Status}.");
        }
    }
}
