using Microsoft.AspNetCore.Components.Authorization;

namespace CodeBeam.UltimateAuth.Client.Authentication
{
    public sealed class UAuthAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly IUAuthClient _client;

        public UAuthAuthenticationStateProvider(IUAuthClient client)
        {
            _client = client;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var principal = await _client.GetCurrentPrincipalAsync();
            return new AuthenticationState(principal);
        }

        public void NotifyStateChanged()
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
    }
}
