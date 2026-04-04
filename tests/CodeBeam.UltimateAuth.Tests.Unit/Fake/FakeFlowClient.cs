using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Client.Services;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;
using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Tests.Unit;

internal sealed class FakeFlowClient : IFlowClient
{
    private readonly Queue<RefreshOutcome> _outcomes;

    public FakeFlowClient(params RefreshOutcome[] outcomes)
    {
        _outcomes = new Queue<RefreshOutcome>(outcomes);
    }

    public Task BeginPkceAsync(bool navigateToHubLogin = true)
    {
        throw new NotImplementedException();
    }

    public Task BeginPkceAsync(string? returnUrl = null)
    {
        throw new NotImplementedException();
    }

    public Task CompletePkceLoginAsync(LoginRequest request)
    {
        throw new NotImplementedException();
    }

    public Task CompletePkceLoginAsync(PkceCompleteRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<ClaimsPrincipal> GetCurrentPrincipalAsync()
    {
        throw new NotImplementedException();
    }

    public Task LoginAsync(LoginRequest request)
    {
        throw new NotImplementedException();
    }

    public Task LoginAsync(LoginRequest request, string? returnUrl)
    {
        throw new NotImplementedException();
    }

    public Task<UAuthResult> LogoutAllDevicesAdminAsync(UserKey userKey)
    {
        throw new NotImplementedException();
    }

    public Task<UAuthResult> LogoutAllDevicesSelfAsync()
    {
        throw new NotImplementedException();
    }

    public Task<UAuthResult> LogoutAllMyDevicesAsync()
    {
        throw new NotImplementedException();
    }

    public Task<UAuthResult> LogoutAllUserDevicesAsync(UserKey userKey)
    {
        throw new NotImplementedException();
    }

    public Task LogoutAsync()
    {
        throw new NotImplementedException();
    }

    public Task<UAuthResult<RevokeResult>> LogoutDeviceAdminAsync(UserKey userKey, SessionChainId chainId)
    {
        throw new NotImplementedException();
    }

    public Task<UAuthResult<RevokeResult>> LogoutDeviceAdminAsync(UserKey userKey, LogoutDeviceRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<UAuthResult<RevokeResult>> LogoutDeviceSelfAsync(LogoutDeviceRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<UAuthResult<RevokeResult>> LogoutMyDeviceAsync(LogoutDeviceRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<UAuthResult> LogoutMyOtherDevicesAsync()
    {
        throw new NotImplementedException();
    }

    public Task<UAuthResult> LogoutOtherDevicesSelfAsync()
    {
        throw new NotImplementedException();
    }

    public Task<UAuthResult<RevokeResult>> LogoutUserDeviceAsync(UserKey userKey, LogoutDeviceRequest request)
    {
        throw new NotImplementedException();
    }

    public Task<UAuthResult> LogoutUserOtherDevicesAsync(UserKey userKey, LogoutOtherDevicesRequest request)
    {
        throw new NotImplementedException();
    }

    public Task NavigateToHubLoginAsync(string authorizationCode, string codeVerifier, string? returnUrl = null)
    {
        throw new NotImplementedException();
    }

    public Task ReauthAsync()
    {
        throw new NotImplementedException();
    }

    public Task<RefreshResult> RefreshAsync(bool isAuto = false)
    {
        var outcome = _outcomes.Count > 0
            ? _outcomes.Dequeue()
            : RefreshOutcome.Success;

        return Task.FromResult(new RefreshResult
        {
            IsSuccess = true,
            Outcome = outcome
        });
    }

    public Task<TryPkceLoginResult> TryCompletePkceLoginAsync(PkceCompleteRequest request, bool commitOnSuccess = false, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<TryPkceLoginResult> TryCompletePkceLoginAsync(PkceCompleteRequest request, UAuthSubmitMode mode)
    {
        throw new NotImplementedException();
    }

    public Task<TryLoginResult> TryLoginAsync(LoginRequest request, UAuthSubmitMode mode, string? returnUrl = null)
    {
        throw new NotImplementedException();
    }

    public Task<AuthValidationResult> ValidateAsync()
    {
        throw new NotImplementedException();
    }
}
