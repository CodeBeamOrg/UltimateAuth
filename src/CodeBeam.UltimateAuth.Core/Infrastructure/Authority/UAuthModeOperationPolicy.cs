using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Core.Infrastructure
{
    public sealed class AuthModeOperationPolicy : IAuthorityPolicy
    {
        public bool AppliesTo(AuthContext context) => true; // Applies to all contexts

        public AuthorizationResult Decide(AuthContext context)
        {
            return context.Mode switch
            {
                UAuthMode.PureOpaque => DecideForPureOpaque(context),
                UAuthMode.PureJwt => DecideForPureJwt(context),
                UAuthMode.Hybrid => AuthorizationResult.Allow(),
                UAuthMode.SemiHybrid => AuthorizationResult.Allow(),

                _ => AuthorizationResult.Deny("Unsupported authentication mode.")
            };
        }

        private static AuthorizationResult DecideForPureOpaque(AuthContext context)
        {
            if (context.Operation == AuthOperation.Refresh)
                return AuthorizationResult.Deny("Refresh operation is not supported in PureOpaque mode.");

            return AuthorizationResult.Allow();
        }

        private static AuthorizationResult DecideForPureJwt(AuthContext context)
        {
            if (context.Operation == AuthOperation.Access)
                return AuthorizationResult.Deny("Session-based access is not supported in PureJwt mode.");

            return AuthorizationResult.Allow();
        }
    }
}
