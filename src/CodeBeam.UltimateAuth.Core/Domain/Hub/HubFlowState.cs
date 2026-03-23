using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Core.Domain;

public sealed class HubFlowState
{
    public HubSessionId HubSessionId { get; init; }
    public HubFlowType FlowType { get; init; }
    public UAuthClientProfile ClientProfile { get; init; }
    public string? ReturnUrl { get; init; }
    public HubErrorCode? Error { get; init; }
    public int AttemptCount { get; init; }

    public bool IsActive { get; init; }
    public bool IsExpired { get; init; }
    public bool IsCompleted { get; init; }
    public bool Exists { get; init; }

    public HubFlowState ClearError()
    {
        return new HubFlowState
        {
            HubSessionId = HubSessionId,
            FlowType = FlowType,
            ClientProfile = ClientProfile,
            ReturnUrl = ReturnUrl,
            Error = null,
            AttemptCount = AttemptCount,
            IsActive = IsActive,
            IsExpired = IsExpired,
            IsCompleted = IsCompleted,
            Exists = Exists
        };
    }
}
