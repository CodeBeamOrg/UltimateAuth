using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Authentication;

internal static class PureJwtDevicePolicy
{
    public static bool IsAllowed(DeviceMismatchBehavior behavior, DeviceId tokenDeviceId, DeviceId? requestDeviceId)
    {
        return behavior switch
        {
            DeviceMismatchBehavior.Reject =>
                requestDeviceId is { } deviceId &&
                deviceId == tokenDeviceId,

            DeviceMismatchBehavior.Allow => true,

            DeviceMismatchBehavior.AllowAndRebind => false,

            _ => false
        };
    }
}
