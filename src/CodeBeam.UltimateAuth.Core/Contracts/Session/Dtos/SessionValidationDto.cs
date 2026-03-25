namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed class SessionValidationDto
{
    public int State { get; set; } = default!;

    public bool IsValid { get; set; }

    public AuthSnapshotDto? Snapshot { get; set; }
}
