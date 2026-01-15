using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Core.Infrastructure
{
    public sealed class DeviceMismatchPolicy : IAuthorityPolicy
    {
        public bool AppliesTo(AuthContext context)
            => context.Device is not null;

        public AuthorizationResult Decide(AuthContext context)
        {
            var device = context.Device;

            //if (device.IsKnownDevice)
            //    return AuthorizationResult.Allow();

            return context.Operation switch
            {
                AuthOperation.Access =>
                    AuthorizationResult.Deny("Access from unknown device."),

                AuthOperation.Refresh =>
                    AuthorizationResult.Challenge("Device verification required."),

                AuthOperation.Login => AuthorizationResult.Allow(), // login establishes device

                _ => AuthorizationResult.Allow()
            };
        }
    }
}
