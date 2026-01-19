using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Users.Contracts
{
    public sealed class ChangeUserStatusRequest
    {
        public required UserKey UserKey { get; init; }
        public required UserStatus NewStatus { get; init; }
    }
}
