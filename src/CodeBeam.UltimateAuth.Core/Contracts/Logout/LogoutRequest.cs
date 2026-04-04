using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed record LogoutRequest
{
    public AuthSessionId SessionId { get; init; }
}
