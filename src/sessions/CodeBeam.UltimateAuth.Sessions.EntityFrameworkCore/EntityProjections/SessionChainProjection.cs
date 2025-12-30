using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore
{
    internal sealed class SessionChainProjection<TUserId>
    {
        public long Id { get; set; }

        public ChainId ChainId { get; set; } = default!;

        public string? TenantId { get; set; }
        public TUserId UserId { get; set; } = default!;

        public int RotationCount { get; set; }
        public long SecurityVersionAtCreation { get; set; }

        public ClaimsSnapshot ClaimsSnapshot { get; set; } = ClaimsSnapshot.Empty;

        public AuthSessionId? ActiveSessionId { get; set; }

        public bool IsRevoked { get; set; }
        public DateTimeOffset? RevokedAt { get; set; }

        public byte[] RowVersion { get; set; } = default!;
    }
}
