namespace CodeBeam.UltimateAuth.Users.Reference;

public sealed class UserProfileQuery
{
    public bool IncludeDeleted { get; init; }

    public int Skip { get; init; }
    public int Take { get; init; } = 50;
}
