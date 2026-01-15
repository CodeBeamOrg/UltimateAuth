using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Core.Domain;

public sealed class HubFlowArtifact : AuthArtifact
{
    public HubSessionId HubSessionId { get; }
    public HubFlowType FlowType { get; }

    public UAuthClientProfile ClientProfile { get; }
    public string? TenantId { get; }
    public string? ReturnUrl { get; }

    public HubFlowPayload Payload { get; }

    public HubFlowArtifact(
        HubSessionId hubSessionId,
        HubFlowType flowType,
        UAuthClientProfile clientProfile,
        string? tenantId,
        string? returnUrl,
        HubFlowPayload payload,
        DateTimeOffset expiresAt)
        : base(AuthArtifactType.HubFlow, expiresAt, maxAttempts: 1)
    {
        HubSessionId = hubSessionId;
        FlowType = flowType;
        ClientProfile = clientProfile;
        TenantId = tenantId;
        ReturnUrl = returnUrl;
        Payload = payload;
    }
}
