using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Core.Contracts;
using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Client
{
    public interface IUAuthClient
    {
        Task LoginAsync(LoginRequest request);
        Task LogoutAsync();
        Task<RefreshResult> RefreshAsync(bool isAuto = false);
        Task ReauthAsync();

        Task<AuthValidationResult> ValidateAsync();
    }
}
