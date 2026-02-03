using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed class DeleteUserRequest
{
    public DeleteMode Mode { get; init; } = DeleteMode.Soft;
}
