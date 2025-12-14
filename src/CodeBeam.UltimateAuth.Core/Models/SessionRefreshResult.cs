using CodeBeam.UltimateAuth.Core.Contexts;

namespace CodeBeam.UltimateAuth.Core.Models
{
    public sealed record SessionRefreshResult
    {
        public IssuedAccessToken AccessToken { get; init; } = default!;
        public IssuedRefreshToken? RefreshToken { get; init; }
    }
}
