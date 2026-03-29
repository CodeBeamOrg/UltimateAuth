namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record ChangeUserStatusAdminRequest
{
    public required AdminAssignableUserStatus NewStatus { get; init; }
}
