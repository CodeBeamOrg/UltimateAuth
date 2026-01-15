using CodeBeam.UltimateAuth.Client.Abstractions;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Principal;

namespace CodeBeam.UltimateAuth.Client.Authentication
{
    internal sealed class UAuthAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly IUAuthStateManager _stateManager;

        public UAuthAuthenticationStateProvider(IUAuthStateManager stateManager)
        {
            _stateManager = stateManager;
            _stateManager.State.Changed += _ => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var principal = _stateManager.State.ToClaimsPrincipal();
            return Task.FromResult(new AuthenticationState(principal));
        }

    }
}
