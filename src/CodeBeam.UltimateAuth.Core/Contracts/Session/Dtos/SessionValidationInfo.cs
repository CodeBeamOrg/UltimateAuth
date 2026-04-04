namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed class SessionValidationInfo
{
    public int State { get; set; } = default!;

    public bool IsValid { get; set; }

    public AuthSnapshotInfo? Snapshot { get; set; }
}
