namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed class AuthSnapshotDto
{
    public IdentityDto? Identity { get; set; }

    public ClaimsDto? Claims { get; set; }
}
