using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;

namespace CodeBeam.UltimateAuth.Docs.Wasm.Client.Layout;

public partial class MainLayout
{
    private const string ThemeStorageKey = "ua-theme";

    [Inject]
    private IJSRuntime JS { get; set; } = default!;

    private bool _drawerOpen = false;
    private bool _isDarkMode = true;
    private bool _visionOverlay = false;

    MudTheme _uauthTheme = new MudTheme()
    {
        PaletteLight = new PaletteLight()
        {
            Primary = "#0C1618",
            Secondary = "#f6f5ae",
            Tertiary = "#8CE38C",
        },

        PaletteDark = new PaletteDark()
        {
            Primary = "#FBFEFB",
            PrimaryContrastText = "#0C1618",
            Secondary = "#2E2D4D",
            Tertiary = "#8CE38C",

            TextPrimary = "#FBFEFB",
            TextSecondary = "#DEF7DE",

            Background = "#0C1618"
        },
    };

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
            return;

        try
        {
            var savedTheme = await JS.InvokeAsync<string?>("localStorage.getItem", ThemeStorageKey);

            if (savedTheme is "dark" or "light")
            {
                _isDarkMode = savedTheme == "dark";
                await InvokeAsync(StateHasChanged);
            }
        }
        catch (JSException)
        {

        }
    }

    public void SetVisionOverlay(bool value)
    {
        _visionOverlay = value;
        StateHasChanged();
    }

    private async Task SaveThemeAsync()
    {
        try
        {
            await JS.InvokeVoidAsync("localStorage.setItem", ThemeStorageKey, _isDarkMode ? "dark" : "light");
        }
        catch (JSException)
        {

        }
    }
}
