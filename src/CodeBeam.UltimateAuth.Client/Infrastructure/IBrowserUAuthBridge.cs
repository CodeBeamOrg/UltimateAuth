namespace CodeBeam.UltimateAuth.Client.Infrastructure;

internal interface IBrowserUAuthBridge
{
    ValueTask SetDeviceIdAsync(string deviceId);
}
