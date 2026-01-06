using Microsoft.AspNetCore.Components.Authorization;

namespace CodeBeam.UltimateAuth.Client.Authentication
{
    public sealed class UAuthAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly IUAuthAuthenticationStateSource _source;

        public UAuthAuthenticationStateProvider(IUAuthAuthenticationStateSource source)
        {
            _source = source;
            _source.StateChanged += () => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync() => _source.GetStateAsync();
    }
}
