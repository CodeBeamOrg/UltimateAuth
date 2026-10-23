namespace CodeBeam.UltimateAuth.IdentityLab.Contracts;

public sealed record SandboxUsersSnapshot(Guid SandboxId, int MaxUsers, IReadOnlyList<SandboxUserItem> Users)
{
    public int MemberCount => Users.Count;
    public int RemainingCapacity => Math.Max(0, MaxUsers - MemberCount);
    public double CapacityPercentage => MaxUsers <= 0 ? 0 : Math.Min(100, (double)MemberCount / MaxUsers * 100);
}
