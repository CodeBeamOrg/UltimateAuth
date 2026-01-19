using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Users.Contracts
{
    public sealed class DeleteUserRequest
    {
        public required UserKey UserKey { get; init; }
        public UserDeleteMode Mode { get; init; } = UserDeleteMode.Soft;
    }
}
