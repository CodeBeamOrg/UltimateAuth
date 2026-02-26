using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Users.Contracts;

public sealed record DeleteUserIdentifierRequest
{
    public Guid IdentifierId { get; set; }
    public DeleteMode Mode { get; set; } = DeleteMode.Soft;
}
