using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Core.Domain;

public sealed class UAuthHubContext
{
    public HubSessionId HubSessionId { get; }
    public HubFlowType FlowType { get; }

    public UAuthClientProfile ClientProfile { get; }
    public string? TenantId { get; }

    public string? ReturnUrl { get; }

    public HubFlowPayload Payload { get; }

    public DateTimeOffset CreatedAt { get; }

    public bool IsCompleted { get; private set; }

    public UAuthHubContext(
        HubSessionId hubSessionId,
        HubFlowType flowType,
        UAuthClientProfile clientProfile,
        string? tenantId,
        string? returnUrl,
        HubFlowPayload payload,
        DateTimeOffset createdAt)
    {
        HubSessionId = hubSessionId;
        FlowType = flowType;
        ClientProfile = clientProfile;
        TenantId = tenantId;
        ReturnUrl = returnUrl;
        Payload = payload;
        CreatedAt = createdAt;
    }

    public void MarkCompleted()
    {
        IsCompleted = true;
    }
}
