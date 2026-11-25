using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Authentication;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server;

public sealed class PureJwtDevicePolicyTests
{
    private static readonly DeviceId TokenDevice = DeviceId.Create("device-token-123456789123456789123456789123456789");
    private static readonly DeviceId OtherDevice = DeviceId.Create("device-other-123456789123456789123456789123456789");

    [Fact]
    public void Reject_WithMatchingDevice_Allows()
    {
        Assert.True(PureJwtDevicePolicy.IsAllowed(DeviceMismatchBehavior.Reject, TokenDevice, TokenDevice));
    }

    [Fact]
    public void Reject_WithDifferentDevice_Denies()
    {
        Assert.False(PureJwtDevicePolicy.IsAllowed(DeviceMismatchBehavior.Reject, TokenDevice, OtherDevice));
    }

    [Fact]
    public void Reject_WithoutRequestDevice_Denies()
    {
        Assert.False(PureJwtDevicePolicy.IsAllowed(DeviceMismatchBehavior.Reject, TokenDevice, null));
    }

    [Fact]
    public void Allow_WithDifferentDevice_Allows()
    {
        Assert.True(PureJwtDevicePolicy.IsAllowed(DeviceMismatchBehavior.Allow, TokenDevice, OtherDevice));
    }

    [Fact]
    public void Allow_WithoutRequestDevice_Allows()
    {
        Assert.True(PureJwtDevicePolicy.IsAllowed(DeviceMismatchBehavior.Allow, TokenDevice, null));
    }

    [Fact]
    public void AllowAndRebind_Denies()
    {
        Assert.False(PureJwtDevicePolicy.IsAllowed(DeviceMismatchBehavior.AllowAndRebind, TokenDevice, TokenDevice));
    }

    [Fact]
    public void UnknownBehavior_Denies()
    {
        Assert.False(PureJwtDevicePolicy.IsAllowed((DeviceMismatchBehavior)999, TokenDevice, TokenDevice));
    }
}
