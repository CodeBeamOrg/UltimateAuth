using CodeBeam.UltimateAuth.Core.Contexts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Models
{
    public sealed record LoginResult
    {
        public bool RequiresMfa { get; init; }
        public string? MfaToken { get; init; }

        public AuthSessionId? SessionId { get; init; }
        public IssuedAccessToken? AccessToken { get; init; }
        public IssuedRefreshToken? RefreshToken { get; init; }
    }

}
