using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record DeleteUserRequest
{
    public DeleteMode Mode { get; init; }
}
