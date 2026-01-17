using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Infrastructure
{
    public sealed class UserRecord<TUserId>
    {
        public TUserId Id { get; init; } = default!;

        /// <summary>
        /// Primary login identifier (username / email).
        /// Used only for discovery and uniqueness checks.
        /// </summary>
        public string Identifier { get; init; } = default!;

        //public UserStatus Status { get; init; } = UserStatus.Active;

        public bool IsDeleted { get; init; }

        public DateTimeOffset CreatedAt { get; init; }

        public DateTimeOffset? DeletedAt { get; init; }

        public bool IsActive { get; init; } = true;
    }


    //public sealed class UserRecord<TUserId>
    //{
    //    public required TUserId Id { get; init; }
    //    public required string Username { get; init; }
    //    public required string PasswordHash { get; init; }
    //    public ClaimsSnapshot Claims { get; init; } = ClaimsSnapshot.Empty;
    //    public bool RequiresMfa { get; init; }
    //    public bool IsActive { get; init; } = true;
    //    public DateTimeOffset CreatedAt { get; init; }
    //    public bool IsDeleted { get; init; }
    //}
}
