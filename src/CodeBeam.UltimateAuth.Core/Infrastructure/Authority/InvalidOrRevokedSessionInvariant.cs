using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Infrastructure
{
    public sealed class InvalidOrRevokedSessionInvariant : IAuthorityInvariant
    {
        public AccessDecisionResult Decide(AuthContext context)
        {
            if (context.Operation == AuthOperation.Login)
                return AccessDecisionResult.Allow();

            var session = context.Session;

            if (session is null)
                return AccessDecisionResult.Deny("Session is required for this operation.");

            if (session.State == SessionState.Invalid ||
                session.State == SessionState.NotFound ||
                session.State == SessionState.Revoked ||
                session.State == SessionState.SecurityMismatch ||
                session.State == SessionState.DeviceMismatch)
            {
                return AccessDecisionResult.Deny($"Session state is invalid: {session.State}");
            }

            return AccessDecisionResult.Allow();
        }
    }
}
