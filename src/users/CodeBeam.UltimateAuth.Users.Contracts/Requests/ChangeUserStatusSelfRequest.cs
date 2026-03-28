namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record ChangeUserStatusSelfRequest
{
    public required SelfUserStatus NewStatus { get; init; }
}
