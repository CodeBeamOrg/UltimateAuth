namespace CodeBeam.UltimateAuth.Client.Options;

internal sealed class ClientConfigurationMarker
{
    private bool _configured;

    public void MarkConfigured()
    {
        if (_configured)
            throw new InvalidOperationException("UltimateAuth client options were configured multiple times. " +
                "Call AddUltimateAuthClient() OR AddUltimateAuthClientBlazor(), not both with configure delegates.");

        _configured = true;
    }
}
