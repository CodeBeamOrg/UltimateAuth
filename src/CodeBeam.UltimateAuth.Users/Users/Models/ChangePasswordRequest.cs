namespace CodeBeam.UltimateAuth.Server.Users
{
    public sealed class ChangePasswordRequest
    {
        public required string CurrentPassword { get; init; }
        public required string NewPassword { get; init; }

        /// <summary>
        /// If true, other sessions will be revoked.
        /// </summary>
        public bool RevokeOtherSessions { get; init; } = true;
    }
}
