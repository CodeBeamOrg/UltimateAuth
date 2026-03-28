using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record ChangeUserStatusAdminRequest
{
    public required UserStatus NewStatus { get; init; }
}
