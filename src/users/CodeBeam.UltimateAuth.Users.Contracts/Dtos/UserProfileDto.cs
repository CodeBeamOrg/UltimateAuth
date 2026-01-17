namespace CodeBeam.UltimateAuth.Users.Contracts
{
    public sealed record UserProfileDto
    {
        public string UserKey { get; init; } = default!;

        public string? UserName { get; init; }
        public string? Email { get; init; }

        public string? FirstName { get; init; }
        public string? LastName { get; init; }
        public string? DisplayName { get; init; }

        public string? Phone { get; init; }

        public bool EmailVerified { get; init; }
        public bool PhoneVerified { get; init; }

        public UserStatus Status { get; init; }
        public DateTimeOffset? CreatedAt { get; init; }
        public DateTimeOffset? LastLoginAt { get; init; }

        public IReadOnlyDictionary<string, string>? Metadata { get; init; }
    }
}
