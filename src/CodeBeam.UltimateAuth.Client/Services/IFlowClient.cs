using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Client.Services;

public interface IFlowClient
{
    Task LoginAsync(LoginRequest request);
    Task LoginAsync(LoginRequest request, string? returnUrl);
    Task LogoutAsync();
    Task<RefreshResult> RefreshAsync(bool isAuto = false);
    Task ReauthAsync();
    Task<AuthValidationResult> ValidateAsync();

    Task BeginPkceAsync(string? returnUrl = null);
    Task CompletePkceLoginAsync(PkceLoginRequest request);
}
