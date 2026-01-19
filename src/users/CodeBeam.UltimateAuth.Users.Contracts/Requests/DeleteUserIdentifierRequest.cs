using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Users.Contracts
{
    public sealed record DeleteUserIdentifierRequest
    {
        public required UserIdentifierType Type { get; init; }
        public required string Value { get; init; }
        public DeleteMode Mode { get; init; } = DeleteMode.Soft;
    }
}
