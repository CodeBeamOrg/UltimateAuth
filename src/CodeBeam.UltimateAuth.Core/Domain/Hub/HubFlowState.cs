using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Core.Domain;

public sealed class HubFlowState
{
    public HubSessionId HubSessionId { get; init; }
    public HubFlowType FlowType { get; init; }
    public UAuthClientProfile ClientProfile { get; init; }
    public string? ReturnUrl { get; init; }

    public bool IsActive { get; init; }
    public bool IsExpired { get; init; }
    public bool IsCompleted { get; init; }
    public bool Exists { get; init; }
}
