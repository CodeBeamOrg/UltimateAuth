using MudBlazor;

namespace CodeBeam.UltimateAuth.Docs.Wasm.Client.Layout;

public partial class MainLayout
{
    private bool _drawerOpen = false;
    private bool _isDarkMode = true;

    MudTheme _uauthTheme = new MudTheme()
    {
        PaletteLight = new PaletteLight()
        {
            Primary = "#0C1618",
            PrimaryContrastText = "#FEEE88",

            Secondary = "#f6f5ae",
        },

        PaletteDark = new PaletteDark()
        {
            Primary = "#FBFEFB",
            Secondary = "#2E2D4D",

            TextPrimary = "#FBFEFB",
            TextSecondary = "#DEF7DE",

            Background = "#0C1618"
        },
    };
}
