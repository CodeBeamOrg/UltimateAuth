namespace CodeBeam.UltimateAuth.Core.Runtime;

/// <summary>
/// Internal marker indicating that UAuthOptions
/// were configured directly by the application.
/// </summary>
internal sealed class DirectCoreConfigurationMarker
{
    public bool IsConfigured { get; private set; }

    public void MarkConfigured()
    {
        IsConfigured = true;
    }
}
