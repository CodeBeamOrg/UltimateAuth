using Microsoft.AspNetCore.Components.Authorization;

namespace CodeBeam.UltimateAuth.Client.Authentication
{
    public sealed class ServerAuthStateSource : IUAuthAuthenticationStateSource
    {
        private readonly AuthenticationStateProvider _provider;

        public event Action? StateChanged;

        public ServerAuthStateSource(AuthenticationStateProvider provider)
        {
            _provider = provider;
            _provider.AuthenticationStateChanged += _ => StateChanged?.Invoke();
        }

        public Task<AuthenticationState> GetStateAsync()
            => _provider.GetAuthenticationStateAsync();
    }
}
