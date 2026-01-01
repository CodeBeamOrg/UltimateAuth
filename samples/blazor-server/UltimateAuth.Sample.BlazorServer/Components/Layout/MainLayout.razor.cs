using MudBlazor;

namespace UltimateAuth.Sample.BlazorServer.Components.Layout
{
    public partial class MainLayout
    {
        private void HandleReauth()
        {
            Snackbar.Add("Reauthentication required. Please log in again.", Severity.Warning);
        }
    }
}
