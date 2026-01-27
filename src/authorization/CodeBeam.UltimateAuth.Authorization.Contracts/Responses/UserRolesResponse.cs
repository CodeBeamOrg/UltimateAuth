using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Authorization.Contracts
{
    public sealed record UserRolesResponse
    {
        public required UserKey UserKey { get; init; }
        public required IReadOnlyCollection<string> Roles { get; init; }
    }

}
