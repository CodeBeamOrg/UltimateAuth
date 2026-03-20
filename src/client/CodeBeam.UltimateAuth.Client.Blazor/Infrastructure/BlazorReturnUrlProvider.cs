using CodeBeam.UltimateAuth.Client.Abstractions;
using Microsoft.AspNetCore.Components;

namespace CodeBeam.UltimateAuth.Client.Blazor.Infrastructure;

internal sealed class BlazorReturnUrlProvider : IReturnUrlProvider
{
    private readonly NavigationManager _nav;

    public BlazorReturnUrlProvider(NavigationManager nav)
    {
        _nav = nav;
    }

    public string GetCurrentUrl() => _nav.Uri;
}
