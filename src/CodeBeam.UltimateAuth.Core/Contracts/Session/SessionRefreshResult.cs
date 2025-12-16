using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed record SessionRefreshResult
    {
        public AccessToken AccessToken { get; init; } = default!;
        public RefreshToken? RefreshToken { get; init; }
    }
}
