using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Server.Auth
{
    public sealed class AuthFlowContext
    {
        public required PrimaryTokenKind PrimaryTokenKind { get; init; }
        public required UAuthClientProfile ClientProfile { get; init; }
        public required UAuthMode EffectiveMode { get; init; }
        public required AuthFlowType FlowType { get; init; }
        public required EffectiveUAuthServerOptions ServerOptions { get; init; }
    }
}
