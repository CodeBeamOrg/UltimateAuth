using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore
{
    internal sealed class SessionProjection<TUserId>
    {
        public long Id { get; set; } // EF internal PK

        public AuthSessionId SessionId { get; set; } = default!;
        public ChainId ChainId { get; set; } = default!;

        public string? TenantId { get; set; }
        public TUserId UserId { get; set; } = default!;

        public DateTimeOffset CreatedAt { get; set; }
        public DateTimeOffset ExpiresAt { get; set; }
        public DateTimeOffset? LastSeenAt { get; set; }

        public bool IsRevoked { get; set; }
        public DateTimeOffset? RevokedAt { get; set; }

        public long SecurityVersionAtCreation { get; set; }

        public DeviceInfo Device { get; set; } = DeviceInfo.Empty;
        public ClaimsSnapshot Claims { get; set; } = ClaimsSnapshot.Empty;
        public SessionMetadata Metadata { get; set; } = SessionMetadata.Empty;

        public byte[] RowVersion { get; set; } = default!;
    }

}
