using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed record LogoutAllRequest
{
    /// <summary>
    /// If true, the current session will NOT be revoked.
    /// </summary>
    public bool ExceptCurrent { get; init; }
}
