namespace CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore
{
    internal sealed class SessionRootProjection<TUserId>
    {
        public long Id { get; set; }

        public string? TenantId { get; set; }
        public TUserId UserId { get; set; } = default!;

        public bool IsRevoked { get; set; }
        public DateTimeOffset? RevokedAt { get; set; }

        public long SecurityVersion { get; set; }
        public DateTimeOffset LastUpdatedAt { get; set; }

        public byte[] RowVersion { get; set; } = default!;
    }
}
