using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Infrastructure
{
    public sealed class ExpiredSessionInvariant : IAuthorityInvariant
    {
        public AuthorizationResult Decide(AuthContext context)
        {
            if (context.Operation == AuthOperation.Login)
                return AuthorizationResult.Allow();

            var session = context.Session;

            if (session is null)
                return AuthorizationResult.Allow();

            if (session.State == SessionState.Expired)
            {
                return AuthorizationResult.Deny("Session has expired.");
            }

            return AuthorizationResult.Allow();
        }
    }
}
