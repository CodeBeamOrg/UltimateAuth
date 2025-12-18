using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Server.Contracts
{
    public sealed record LoginResponse
    {
        public string? SessionId { get; init; }
        public AccessToken? AccessToken { get; init; }
        public RefreshToken? RefreshToken { get; init; }
        public object? Continuation { get; init; }
    }
}
