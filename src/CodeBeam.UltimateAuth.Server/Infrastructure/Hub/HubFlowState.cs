using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public sealed record HubFlowState(
        bool Exists,
        bool IsActive,
        bool IsCompleted,
        bool IsExpired,
        HubFlowType? FlowType,
        UAuthClientProfile? ClientProfile,
        string? ReturnUrl
    );

}
