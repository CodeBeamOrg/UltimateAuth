using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed record RefreshFlowRequest
{
    public AuthSessionId? SessionId { get; init; }
    public string? RefreshToken { get; init; }
    public required DeviceContext Device { get; init; }
    public SessionTouchMode TouchMode { get; init; } = SessionTouchMode.IfNeeded;
}
