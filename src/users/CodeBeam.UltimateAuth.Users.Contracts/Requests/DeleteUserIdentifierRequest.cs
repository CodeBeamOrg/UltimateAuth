using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record DeleteUserIdentifierRequest
{
    public Guid Id { get; init; }
    public DeleteMode Mode { get; init; }
}
