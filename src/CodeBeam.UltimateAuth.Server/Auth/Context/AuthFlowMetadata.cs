using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Auth;

public sealed class AuthFlowMetadata
{
    public AuthFlowType FlowType { get; }

    public AuthFlowMetadata(AuthFlowType flowType)
    {
        FlowType = flowType;
    }
}
