namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record ChangeUserStatusSelfRequest
{
    public required SelfAssignableUserStatus NewStatus { get; init; }
}
