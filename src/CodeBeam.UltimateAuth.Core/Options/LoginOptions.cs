namespace CodeBeam.UltimateAuth.Core.Options
{
    public sealed class LoginOptions
    {
        /// <summary>
        /// Maximum failed login attempts before temporary lockout.
        /// </summary>
        public int MaxFailedAttempts { get; set; } = 5;

        /// <summary>
        /// Lockout duration in minutes.
        /// </summary>
        public int LockoutMinutes { get; set; } = 15;
    }
}
