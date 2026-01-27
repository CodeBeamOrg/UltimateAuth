using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Credentials.Contracts
{
    public sealed class RevokeAllCredentialsRequest
    {
        public required UserKey UserKey { get; init; }
    }
}
