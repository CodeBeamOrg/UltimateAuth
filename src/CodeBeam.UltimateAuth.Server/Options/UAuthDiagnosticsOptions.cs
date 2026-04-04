namespace CodeBeam.UltimateAuth.Server.Options;

public sealed class UAuthDiagnosticsOptions
{
    /// <summary>
    /// Enables debug / sample-only response headers such as X-UAuth-Refresh. If true, gives succesful refresh details.
    /// Better to be disabled in production.
    /// </summary>
    public bool EnableRefreshDetails { get; set; } = false;

    internal UAuthDiagnosticsOptions Clone() => new()
    {
        EnableRefreshDetails = EnableRefreshDetails
    };
}
