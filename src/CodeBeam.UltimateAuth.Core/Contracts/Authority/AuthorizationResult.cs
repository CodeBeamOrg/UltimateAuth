namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed class AuthorizationResult
    {
        public AuthorizationDecision Decision { get; }
        public string? Reason { get; }

        private AuthorizationResult(AuthorizationDecision decision, string? reason)
        {
            Decision = decision;
            Reason = reason;
        }

        public static AuthorizationResult Allow()
            => new(AuthorizationDecision.Allow, null);

        public static AuthorizationResult Deny(string reason)
            => new(AuthorizationDecision.Deny, reason);

        public static AuthorizationResult Challenge(string reason)
            => new(AuthorizationDecision.Challenge, reason);

        // Developer happiness helpers
        public bool IsAllowed => Decision == AuthorizationDecision.Allow;
        public bool IsDenied => Decision == AuthorizationDecision.Deny;
        public bool RequiresChallenge => Decision == AuthorizationDecision.Challenge;
    }

}
