namespace CodeBeam.UltimateAuth.Client.Infrastructure;

public interface IBrowserUAuthBridge
{
    ValueTask SetDeviceIdAsync(string deviceId);
}
