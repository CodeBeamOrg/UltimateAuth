using Microsoft.AspNetCore.Components;

namespace CodeBeam.UltimateAuth.Tests.Unit;

internal sealed class TestNavigationManager : NavigationManager
{
    public string? LastNavigatedTo { get; private set; }

    public TestNavigationManager()
    {
        Initialize("http://localhost/", "http://localhost/");
    }

    protected override void NavigateToCore(string uri, bool forceLoad)
    {
        LastNavigatedTo = uri;
    }
}
