using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Tests.Unit.Helpers;

internal static class TestDevice
{
    public static DeviceContext Default() => DeviceContext.Create(DeviceId.Create("test-device-000-000-000-000-01"), null, null, null, null, null);
}
