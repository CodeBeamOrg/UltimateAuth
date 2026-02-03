namespace CodeBeam.UltimateAuth.Users.Contracts;

public class ChangeUserStatusSelfRequest
{
    public required UserStatus NewStatus { get; init; }
}
