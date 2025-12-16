namespace CodeBeam.UltimateAuth.Server.Users
{
    public sealed class UserDto<TUserId>
    {
        public required TUserId UserId { get; init; }

        public string? Username { get; init; }
        public string? Email { get; init; }

        public bool IsActive { get; init; }
        public bool IsEmailConfirmed { get; init; }

        public DateTime CreatedAt { get; init; }
        public DateTime? LastLoginAt { get; init; }
    }
}
