namespace CodeBeam.UltimateAuth.Users.Contracts;

public class ChangeUserStatusSelfRequest
{
    public required SelfUserStatus NewStatus { get; init; }
}
