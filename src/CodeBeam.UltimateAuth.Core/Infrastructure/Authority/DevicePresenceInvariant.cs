using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Core.Infrastructure
{
    public sealed class DevicePresenceInvariant : IAuthorityInvariant
    {
        public AuthorizationResult Decide(AuthContext context)
        {
            if (context.Operation is AuthOperation.Login or AuthOperation.Refresh)
            {
                if (context.Device is null)
                    return AuthorizationResult.Deny("Device information is required.");
            }

            return AuthorizationResult.Allow();
        }
    }

}
