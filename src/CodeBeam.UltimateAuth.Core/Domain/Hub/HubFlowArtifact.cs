using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Core.Domain;

public sealed class HubFlowArtifact : AuthArtifact
{
    public HubSessionId HubSessionId { get; }
    public HubFlowType FlowType { get; }

    public UAuthClientProfile ClientProfile { get; }
    public TenantKey Tenant { get; }
    public string? ReturnUrl { get; }

    public HubFlowPayload Payload { get; }

    public string? Error { get; private set; }

    public HubFlowArtifact(
        HubSessionId hubSessionId,
        HubFlowType flowType,
        UAuthClientProfile clientProfile,
        TenantKey tenant,
        string? returnUrl,
        HubFlowPayload payload,
        DateTimeOffset expiresAt)
        : base(AuthArtifactType.HubFlow, expiresAt)
    {
        HubSessionId = hubSessionId;
        FlowType = flowType;
        ClientProfile = clientProfile;
        Tenant = tenant;
        ReturnUrl = returnUrl;
        Payload = payload;
    }

    public void SetError(string error)
    {
        Error = error;
        RegisterAttempt();
    }

    public void ClearError()
    {
        Error = null;
        RegisterAttempt();
    }
}
