namespace CodeBeam.UltimateAuth.Server.Users
{
    public sealed class ConfigureMfaRequest
    {
        public bool Enable { get; init; }

        /// <summary>
        /// Optional verification code when enabling MFA.
        /// </summary>
        public string? VerificationCode { get; init; }
    }
}
