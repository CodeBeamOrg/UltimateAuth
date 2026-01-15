namespace CodeBeam.UltimateAuth.Core.Domain
{
    public sealed record DeviceContext
    {
        public required DeviceId DeviceId { get; init; }

        // DeviceInfo is a transport object.
        // AuthFlowContextFactory changes it to a useable DeviceContext
        // DeviceContext doesn't have fields like IsTrusted etc. It's authority layer's responsibility.
        // IP, Geo, Fingerprint, Platform, UA will be added here.

    }

}
