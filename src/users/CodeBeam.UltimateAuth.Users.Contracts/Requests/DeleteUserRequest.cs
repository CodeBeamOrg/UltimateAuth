using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Users.Contracts
{
    public sealed class DeleteUserRequest
    {
        public required UserKey UserKey { get; init; }
        public DeleteMode Mode { get; init; } = DeleteMode.Soft;
    }
}
