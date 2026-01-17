namespace CodeBeam.UltimateAuth.Credentials.Contracts
{
    public sealed record ResetPasswordRequest
    {
        public string UserKey { get; init; } = default!;
        public required string NewPassword { get; init; }

        /// <summary>
        /// Optional reset token or verification code.
        /// </summary>
        public string? Token { get; init; }
    }
}
