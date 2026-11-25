namespace CodeBeam.UltimateAuth.Server.Options;

/// <summary>
/// Determines how a new device is handled when a device limit is reached.
/// </summary>
public enum DeviceLimitBehavior
{
    /// <summary>
    /// Rejects authentication from a new device without revoking any existing device sessions.
    /// </summary>
    RejectNewLogin = 0,

    /// <summary>
    /// Revokes the oldest eligible device sessions to make room for the new device.
    /// </summary>
    RevokeOldest = 1
}
