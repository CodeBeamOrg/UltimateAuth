using Microsoft.AspNetCore.Components.Authorization;

namespace CodeBeam.UltimateAuth.Client.Authentication
{
    public sealed class ClientAuthStateSource : IUAuthAuthenticationStateSource
    {
        private readonly IUAuthClient _client;

        public event Action? StateChanged;

        public ClientAuthStateSource(IUAuthClient client)
        {
            _client = client;
        }

        public async Task<AuthenticationState> GetStateAsync()
        {
            var principal = await _client.GetCurrentPrincipalAsync();
            return new AuthenticationState(principal);
        }

        public void NotifyChanged() => StateChanged?.Invoke();
    }
}
