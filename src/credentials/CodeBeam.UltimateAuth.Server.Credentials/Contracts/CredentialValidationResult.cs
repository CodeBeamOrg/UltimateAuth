namespace CodeBeam.UltimateAuth.Server.Credentials
{
    public sealed record CredentialValidationResult(
        bool IsValid,
        bool RequiresReauthentication,
        bool RequiresSecurityVersionIncrement,
        string? FailureReason = null)
    {
        public static CredentialValidationResult Success(
            bool requiresSecurityVersionIncrement = false)
            => new(
                IsValid: true,
                RequiresReauthentication: false,
                RequiresSecurityVersionIncrement: requiresSecurityVersionIncrement);

        public static CredentialValidationResult Failed(
            string? reason = null,
            bool requiresReauthentication = false)
            => new(
                IsValid: false,
                RequiresReauthentication: requiresReauthentication,
                RequiresSecurityVersionIncrement: false,
                FailureReason: reason);

        public static CredentialValidationResult ReauthenticationRequired(
            string? reason = null)
            => new(
                IsValid: false,
                RequiresReauthentication: true,
                RequiresSecurityVersionIncrement: false,
                FailureReason: reason);
    }
}
