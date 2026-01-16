using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Users.InMemory
{
    internal sealed class InMemoryUser<TUserId>
    {
        public TUserId Id { get; init; } = default!;
        public string Login { get; init; } = default!;
        public bool IsActive { get; set; } = true;
        public bool IsDeleted { get; set; }

        public DateTimeOffset CreatedAt { get; init; } = DateTimeOffset.UtcNow;

        public ClaimsSnapshot Claims { get; set; } = ClaimsSnapshot.Empty;
    }
}
