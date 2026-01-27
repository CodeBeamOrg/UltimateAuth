namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed class AccessDecisionResult
    {
        public AuthorizationDecision Decision { get; }
        public string? Reason { get; }

        private AccessDecisionResult(AuthorizationDecision decision, string? reason)
        {
            Decision = decision;
            Reason = reason;
        }

        public static AccessDecisionResult Allow()
            => new(AuthorizationDecision.Allow, null);

        public static AccessDecisionResult Deny(string reason)
            => new(AuthorizationDecision.Deny, reason);

        public static AccessDecisionResult Challenge(string reason)
            => new(AuthorizationDecision.Challenge, reason);

        // Developer happiness helpers
        public bool IsAllowed => Decision == AuthorizationDecision.Allow;
        public bool IsDenied => Decision == AuthorizationDecision.Deny;
        public bool RequiresChallenge => Decision == AuthorizationDecision.Challenge;
    }

}
