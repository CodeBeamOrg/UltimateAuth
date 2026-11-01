using CodeBeam.UltimateAuth.Client.Events;
using CodeBeam.UltimateAuth.Client.Errors;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using System.Net;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Client.Authentication;

internal sealed class UAuthStateManager : IUAuthStateManager, IDisposable
{
    private Task? _ensureTask;
    private DateTimeOffset _validationNotBefore;
    private readonly object _ensureLock = new();

    private readonly IUAuthClient _client;
    private readonly IUAuthClientEvents _events;
    private readonly IClock _clock;

    public UAuthState State { get; } = UAuthState.Anonymous();

    public UAuthStateManager(IUAuthClient client, IUAuthClientEvents events, IClock clock)
    {
        _client = client;
        _events = events;
        _clock = clock;

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
        catch (UAuthTransportException ex) when (ex.StatusCode == HttpStatusCode.TooManyRequests)
        {
            _validationNotBefore = UAuthRateLimit.NotBefore(_clock.UtcNow, ex.RetryAfter);
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
        if (args.RefreshMode == UAuthStateEventHandlingMode.None)
        {
            return;
        }

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

            case UAuthStateEventArgs<ChangeUserStatusSelfRequest> profile:
                State.UpdateUserStatus(profile.Payload);
                return;
        }

        switch (args.RefreshMode)
        {
            case UAuthStateEventHandlingMode.Validate:
                await EnsureAsync(true);
                return;

            case UAuthStateEventHandlingMode.Patch:
                if (args.Type == UAuthStateEvent.ValidationCalled)
                {
                    State.MarkValidated(_clock.UtcNow);
                    return;
                }
                break;
            default:
                break;
        }

        State.Touch(true);
    }

    public Task OnLoginAsync()
    {
        State.MarkStale();
        return Task.CompletedTask;
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
}
