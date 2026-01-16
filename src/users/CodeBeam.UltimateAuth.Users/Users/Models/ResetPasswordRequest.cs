namespace CodeBeam.UltimateAuth.Users
{
    public sealed class ResetPasswordRequest
    {
        public required string NewPassword { get; init; }

        /// <summary>
        /// If true, all active sessions will be revoked.
        /// </summary>
        public bool RevokeSessions { get; init; } = true;
    }
}
