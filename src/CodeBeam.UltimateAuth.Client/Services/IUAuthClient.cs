using CodeBeam.UltimateAuth.Client.Contracts;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Client
{
    public interface IUAuthClient
    {
        Task LoginAsync(LoginRequest request);
        Task LogoutAsync();
        Task RefreshAsync();
        Task ReauthAsync();

        Task<AuthValidationResult> ValidateAsync();
    }

}
