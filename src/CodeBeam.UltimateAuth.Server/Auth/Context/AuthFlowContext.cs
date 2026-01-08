using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Options;

namespace CodeBeam.UltimateAuth.Server.Auth
{
    public sealed class AuthFlowContext
    {

        public AuthFlowType FlowType { get; }
        public UAuthClientProfile ClientProfile { get; }
        public UAuthMode EffectiveMode { get; }


        public string? TenantId { get; }
        public bool IsAuthenticated { get; }
        public UserId? UserId { get; }
        public AuthSessionId? SessionId { get; }


        public UAuthServerOptions OriginalOptions { get; }
        public EffectiveUAuthServerOptions EffectiveOptions { get; }


        public EffectiveAuthResponse Response { get; }
        public PrimaryTokenKind PrimaryTokenKind { get; }

        // Helpers
        public bool AllowsTokenIssuance =>
            Response.AccessTokenDelivery.Mode != TokenResponseMode.None ||
            Response.RefreshTokenDelivery.Mode != TokenResponseMode.None;

        internal AuthFlowContext(
            AuthFlowType flowType,
            UAuthClientProfile clientProfile,
            UAuthMode effectiveMode,
            string? tenantId,
            bool isAuthenticated,
            UserId? userId,
            AuthSessionId? sessionId,
            UAuthServerOptions originalOptions,
            EffectiveUAuthServerOptions effectiveOptions,
            EffectiveAuthResponse response,
            PrimaryTokenKind primaryTokenKind)
        {
            FlowType = flowType;
            ClientProfile = clientProfile;
            EffectiveMode = effectiveMode;

            TenantId = tenantId;
            IsAuthenticated = isAuthenticated;
            UserId = userId;
            SessionId = sessionId;

            OriginalOptions = originalOptions;
            EffectiveOptions = effectiveOptions;

            Response = response;
            PrimaryTokenKind = primaryTokenKind;
        }
    }
}
