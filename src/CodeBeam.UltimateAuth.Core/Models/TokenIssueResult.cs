using CodeBeam.UltimateAuth.Core.Contexts;

namespace CodeBeam.UltimateAuth.Core.Models
{
    public sealed record TokenIssueResult
    {
        public IssuedAccessToken AccessToken { get; init; } = default!;

        public IssuedRefreshToken? RefreshToken { get; init; }

        public static TokenIssueResult From(IssuedAccessToken access, IssuedRefreshToken? refresh)
            => new() { AccessToken = access, RefreshToken = refresh };
    }
}
