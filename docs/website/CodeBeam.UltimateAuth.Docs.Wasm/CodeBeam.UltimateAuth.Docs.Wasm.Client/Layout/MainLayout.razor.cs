using MudBlazor;

namespace CodeBeam.UltimateAuth.Docs.Wasm.Client.Layout;

public partial class MainLayout
{
    private bool _drawerOpen = false;
    private bool _isDarkMode = true;
    private bool _visionOverlay = false;

    public void SetVisionOverlay(bool value)
    {
        _visionOverlay = value;
        StateHasChanged();
    }

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
}
