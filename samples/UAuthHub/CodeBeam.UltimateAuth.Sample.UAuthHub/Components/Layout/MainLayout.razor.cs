namespace CodeBeam.UltimateAuth.Sample.UAuthHub.Components.Layout
{
    public partial class MainLayout
    {
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await HubContextInitializer.EnsureInitializedAsync();
            }
        }
    }
}
