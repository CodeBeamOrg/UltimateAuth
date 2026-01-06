using Microsoft.AspNetCore.Components.Authorization;

namespace CodeBeam.UltimateAuth.Client.Authentication
{
    public interface IUAuthAuthenticationStateSource
    {
        Task<AuthenticationState> GetStateAsync();
        event Action? StateChanged;
    }
}
