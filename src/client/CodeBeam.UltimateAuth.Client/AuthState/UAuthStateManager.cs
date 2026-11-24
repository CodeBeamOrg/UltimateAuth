using CodeBeam.UltimateAuth.Client.Abstractions;
using CodeBeam.UltimateAuth.Client.Errors;
using CodeBeam.UltimateAuth.Client.Events;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Client.Options;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;
using Microsoft.Extensions.Options;
using System.Net;

namespace CodeBeam.UltimateAuth.Client.Authentication;

internal sealed class UAuthStateManager : IUAuthStateManager, IDisposable
{
    private readonly IUAuthClient _client;
    private readonly IUAuthClientEvents _events;
    private readonly IClock _clock;
    private readonly IUAuthTokenStore _tokenStore;
    private readonly IUAuthRequestClient _requestClient;
    private readonly UAuthClientOptions _options;

    private Task? _ensureTask;
    private DateTimeOffset _validationNotBefore;
    private readonly object _ensureLock = new();
    private bool _initialRecoveryCompleted;

    public UAuthState State { get; } = UAuthState.Anonymous();

    public UAuthStateManager(
        IUAuthClient client,
        IUAuthClientEvents events,
        IClock clock,
        IUAuthTokenStore tokenStore,
        IUAuthRequestClient requestClient,
        IOptions<UAuthClientOptions> options)
    {
        _client = client;
        _events = events;
        _clock = clock;
        _tokenStore = tokenStore;
        _requestClient = requestClient;
        _options = options.Value;

        _events.StateChanged += HandleStateEvent;
    }

    public Task EnsureAsync(bool force = false, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (_clock.UtcNow < _validationNotBefore) return Task.CompletedTask;
        if (!force && State.IsAuthenticated && !State.IsStale)
            return Task.CompletedTask;

        lock (_ensureLock)
        {
            if (_ensureTask is { IsCompleted: false })
                return _ensureTask;

            _ensureTask = EnsureInternalAsync(ct);
            return _ensureTask;
        }
    }

    private async Task EnsureInternalAsync(CancellationToken ct)
    {
        try
        {
            var result = await _client.Flows.ValidateAsync();

            if (!_initialRecoveryCompleted)
            {
                if (result.State == SessionState.NotFound)
                {
                    var recovered =
                        await TryRecoverAuthenticationAsync(ct);

                    if (recovered)
                    {
                        result = await _client.Flows.ValidateAsync();
                    }
                }

                _initialRecoveryCompleted = true;
            }

            if (!result.IsValid)
            {
                State.Clear();
                return;
            }

            if (result.Snapshot is null)
            {
                State.MarkStale();
                return;
            }

            State.ApplySnapshot(result.Snapshot, _clock.UtcNow);
        }
        catch (UAuthTransportException ex)
            when (ex.StatusCode == HttpStatusCode.TooManyRequests)
        {
            _validationNotBefore = UAuthRateLimit.NotBefore(
                _clock.UtcNow,
                ex.RetryAfter);
        }
        finally
        {
            lock (_ensureLock)
            {
                _ensureTask = null;
            }
        }
    }

    private async Task HandleStateEvent(UAuthStateEventArgs args)
    {
        switch (args.Type)
        {
            case UAuthStateEvent.LoginSucceeded:
                await EnsureAsync(force: true);
                return;

            case UAuthStateEvent.ReauthenticationRequired:
                State.Clear();
                return;
        }

        if (args.RefreshMode == UAuthStateEventHandlingMode.None)
            return;

        switch (args.Type)
        {
            case UAuthStateEvent.SessionRevoked:
            case UAuthStateEvent.CredentialsChanged:
            case UAuthStateEvent.UserDeleted:
            case UAuthStateEvent.LogoutVariant:
                State.Clear();
                return;
        }

        switch (args)
        {
            case UAuthStateEventArgs<UpdateProfileRequest> profile:
                State.UpdateProfile(profile.Payload);
                return;

            case UAuthStateEventArgs<ChangeUserStatusSelfRequest> status:
                State.UpdateUserStatus(status.Payload);
                return;
        }

        switch (args.RefreshMode)
        {
            case UAuthStateEventHandlingMode.Validate:
                await EnsureAsync(force: true);
                return;

            case UAuthStateEventHandlingMode.Patch:
                if (args.Type == UAuthStateEvent.ValidationCalled)
                {
                    State.MarkValidated(_clock.UtcNow);
                    return;
                }

                break;
        }

        State.Touch(true);
    }

    public Task OnLoginAsync()
    {
        return EnsureAsync(force: true);
    }

    public Task OnLogoutAsync()
    {
        State.Clear();
        return Task.CompletedTask;
    }

    public void MarkStale()
    {
        State.MarkStale();
    }

    public void Clear()
    {
        State.Clear();
    }

    public void Dispose()
    {
        _events.StateChanged -= HandleStateEvent;
    }

    public bool NeedsValidation => !State.IsAuthenticated || State.IsStale;

    private async Task<bool> TryRecoverAuthenticationAsync(CancellationToken ct)
    {
        var existingToken = _tokenStore.Read();

        if (existingToken is not null && !existingToken.IsExpired(_clock.UtcNow))
        {
            return false;
        }

        var transportUrl = UAuthUrlBuilder.Build(_options.Endpoints.BasePath, _options.Endpoints.LoginTransport, _options.MultiTenant);

        var transport = await _requestClient.GetLoginTransportAsync(transportUrl, ct);

        if (!transport.Supported || !transport.AccessTokenInHeader)
        {
            return false;
        }

        var refresh = await _client.Flows.RecoverAsync();

        if (refresh.Status == 429)
        {
            throw UAuthRateLimit.Exception(
                refresh.RetryAfter is { } retryAfter
                    ? Math.Ceiling(retryAfter.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture)
                    : null);
        }

        if (!refresh.IsSuccess)
        {
            return false;
        }

        var recoveredToken = _tokenStore.Read();

        return recoveredToken is not null && !recoveredToken.IsExpired(_clock.UtcNow);
    }
}
