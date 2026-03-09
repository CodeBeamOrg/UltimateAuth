namespace CodeBeam.UltimateAuth.Core.Domain;

public sealed record UserRuntimeRecord
{
    public UserKey UserKey { get; init; }
    public bool IsActive { get; init; }
    public bool CanAuthenticate { get; init; }
    public bool IsDeleted { get; init; }
    public bool Exists { get; init; }
}
