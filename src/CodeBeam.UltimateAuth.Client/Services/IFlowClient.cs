using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Core.Contracts;

// TODO: Add ReauthAsync
namespace CodeBeam.UltimateAuth.Client.Services;

public interface IFlowClient
{
    Task LoginAsync(LoginRequest request, string? returnUrl = null);
    Task LogoutAsync();
    Task<RefreshResult> RefreshAsync(bool isAuto = false);
    //Task ReauthAsync();
    Task<AuthValidationResult> ValidateAsync();

    Task BeginPkceAsync(string? returnUrl = null);
    Task CompletePkceLoginAsync(PkceLoginRequest request);
}
