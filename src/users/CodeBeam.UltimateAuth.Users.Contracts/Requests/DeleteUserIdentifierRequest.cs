using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record DeleteUserIdentifierRequest
{
    public Guid IdentifierId { get; init; }
    public DeleteMode Mode { get; init; } = DeleteMode.Soft;
}
