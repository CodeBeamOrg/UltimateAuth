using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed class ChangeUserStatusAdminRequest
{
    public required UserStatus NewStatus { get; init; }
}
