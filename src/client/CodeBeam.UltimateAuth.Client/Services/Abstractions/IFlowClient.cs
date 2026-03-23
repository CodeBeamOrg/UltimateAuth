using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;

// TODO: Add ReauthAsync
namespace CodeBeam.UltimateAuth.Client.Services;

public interface IFlowClient
{
    Task LoginAsync(LoginRequest request, string? returnUrl = null);
    Task<TryLoginResult> TryLoginAsync(LoginRequest request, UAuthSubmitMode mode, string? returnUrl = null);

    Task LogoutAsync();
    Task<RefreshResult> RefreshAsync(bool isAuto = false);
    //Task ReauthAsync();
    Task<AuthValidationResult> ValidateAsync();

    Task BeginPkceAsync(string? returnUrl = null);
    Task<PkceCredentials> ContinuePkceAsync(HubFlowArtifact hub);
    Task<PkceCredentials> BeginPkceSilentAsync();
    Task<TryPkceLoginResult> TryCompletePkceLoginAsync(PkceCompleteRequest request, UAuthSubmitMode mode);
    Task CompletePkceLoginAsync(PkceCompleteRequest request);

    Task<UAuthResult<RevokeResult>> LogoutDeviceSelfAsync(LogoutDeviceRequest request);
    Task<UAuthResult> LogoutOtherDevicesSelfAsync();
    Task<UAuthResult> LogoutAllDevicesSelfAsync();
    Task<UAuthResult<RevokeResult>> LogoutDeviceAdminAsync(UserKey userKey, LogoutDeviceRequest request);
    Task<UAuthResult> LogoutOtherDevicesAdminAsync(UserKey userKey, LogoutOtherDevicesAdminRequest request);
    Task<UAuthResult> LogoutAllDevicesAdminAsync(UserKey userKey);
}
