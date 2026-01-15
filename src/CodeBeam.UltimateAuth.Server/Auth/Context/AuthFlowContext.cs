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
        public DeviceContext Device { get; }

        public string? TenantId { get; }
        public SessionSecurityContext? Session { get; }
        public bool IsAuthenticated { get; }
        public UserKey? UserKey { get; }

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
            DeviceContext device,
            string? tenantId,
            bool isAuthenticated,
            UserKey? userKey,
            SessionSecurityContext? session,
            UAuthServerOptions originalOptions,
            EffectiveUAuthServerOptions effectiveOptions,
            EffectiveAuthResponse response,
            PrimaryTokenKind primaryTokenKind)
        {
            FlowType = flowType;
            ClientProfile = clientProfile;
            EffectiveMode = effectiveMode;
            Device = device;

            TenantId = tenantId;
            Session = session;
            IsAuthenticated = isAuthenticated;
            UserKey = userKey;

            OriginalOptions = originalOptions;
            EffectiveOptions = effectiveOptions;

            Response = response;
            PrimaryTokenKind = primaryTokenKind;
        }
    }
}
