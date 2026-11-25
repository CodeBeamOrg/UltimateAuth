namespace CodeBeam.UltimateAuth.Client;

public sealed class UAuthLoginTransportInfo
{
    public bool Supported { get; init; }
    public bool NavigationSupported { get; init; }
    public bool AccessTokenInHeader { get; init; }
}
