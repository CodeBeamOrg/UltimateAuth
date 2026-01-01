using Microsoft.AspNetCore.Components.Authorization;

namespace CodeBeam.UltimateAuth.Client.BlazorServer;

internal sealed class UAuthAuthenticationStateProvider : AuthenticationStateProvider
{
    private readonly AuthenticationStateProvider _inner;

    public UAuthAuthenticationStateProvider(AuthenticationStateProvider inner)
    {
        _inner = inner;

        _inner.AuthenticationStateChanged += s => NotifyAuthenticationStateChanged(s);
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync() => _inner.GetAuthenticationStateAsync();

    /// <summary>
    /// Call this after login/logout navigation
    /// </summary>
    public void NotifyStateChanged() => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
}
