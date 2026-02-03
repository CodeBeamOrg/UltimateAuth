using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Users.Reference;

public sealed class UserLifecycleQuery
{
    public bool IncludeDeleted { get; init; }
    public UserStatus? Status { get; init; }

    public int Skip { get; init; }
    public int Take { get; init; } = 50;
}
