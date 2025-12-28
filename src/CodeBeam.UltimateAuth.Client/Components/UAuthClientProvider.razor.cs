namespace CodeBeam.UltimateAuth.Client
{
    // TODO: Add CircuitHandler to manage start/stop of coordinator in server-side Blazor
    public partial class UAuthClientProvider
    {
        private bool _started;

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (!firstRender || _started)
                return;

            _started = true;
            await Coordinator.StartAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await Coordinator.StopAsync();
        }
    }
}
