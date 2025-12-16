using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Core.Infrastructure
{
    public sealed class DeviceTrustPolicy : IAuthorityPolicy
    {
        public bool AppliesTo(AuthContext context) => context.Device is not null;

        public AuthorizationResult Decide(AuthContext context)
        {
            var device = context.Device;

            if (device.IsTrusted)
                return AuthorizationResult.Allow();

            return context.Operation switch
            {
                AuthOperation.Login =>
                    AuthorizationResult.Challenge("Login from untrusted device requires additional verification."),

                AuthOperation.Refresh =>
                    AuthorizationResult.Challenge("Token refresh from untrusted device requires additional verification."),

                AuthOperation.Access =>
                    AuthorizationResult.Deny("Access from untrusted device is not allowed."),

                _ => AuthorizationResult.Allow()
            };
        }
    }
}
