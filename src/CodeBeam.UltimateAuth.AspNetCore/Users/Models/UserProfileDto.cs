namespace CodeBeam.UltimateAuth.Server.Users
{
    public sealed class UserProfileDto<TUserId>
    {
        public required TUserId UserId { get; init; }

        public string? Username { get; init; }
        public string? Email { get; init; }

        public bool IsEmailConfirmed { get; init; }

        public DateTime CreatedAt { get; init; }
    }
}
