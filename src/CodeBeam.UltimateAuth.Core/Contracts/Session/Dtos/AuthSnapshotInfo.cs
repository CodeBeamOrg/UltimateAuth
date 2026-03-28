namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed class AuthSnapshotInfo
{
    public IdentityInfo? Identity { get; set; }

    public ClaimsInfo? Claims { get; set; }
}
