using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Infrastructure
{
    public sealed class UserRecord<TUserId>
    {
        public required TUserId Id { get; init; }
        public required string Username { get; init; }
        public required string PasswordHash { get; init; }
        public ClaimsSnapshot Claims { get; init; } = ClaimsSnapshot.Empty;
        public bool RequiresMfa { get; init; }
        public bool IsActive { get; init; } = true;
        public DateTime CreatedAt { get; init; }
        public bool IsDeleted { get; init; }
    }
}
