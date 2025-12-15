using CodeBeam.UltimateAuth.Core.Contexts;

namespace CodeBeam.UltimateAuth.Core.Models
{
    public sealed record SessionRefreshResult
    {
        public AccessToken AccessToken { get; init; } = default!;
        public RefreshToken? RefreshToken { get; init; }
    }
}
