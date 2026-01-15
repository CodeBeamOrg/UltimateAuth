namespace CodeBeam.UltimateAuth.Core.Domain
{
    public sealed class DeviceContext
    {
        public DeviceId? DeviceId { get; init; }

        public bool HasDeviceId => DeviceId is not null;

        private DeviceContext(DeviceId? deviceId)
        {
            DeviceId = deviceId;
        }

        public static DeviceContext Anonymous()
            => new(null);

        public static DeviceContext FromDeviceId(DeviceId deviceId)
            => new(deviceId);

        // DeviceInfo is a transport object.
        // AuthFlowContextFactory changes it to a useable DeviceContext
        // DeviceContext doesn't have fields like IsTrusted etc. It's authority layer's responsibility.
        // IP, Geo, Fingerprint, Platform, UA will be added here.

    }

}
