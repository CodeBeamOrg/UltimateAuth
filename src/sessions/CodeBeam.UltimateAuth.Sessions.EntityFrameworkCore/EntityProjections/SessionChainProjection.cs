using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Sessions.EntityFrameworkCore
{
    internal sealed class SessionChainProjection
    {
        public long Id { get; set; }

        public SessionChainId ChainId { get; set; } = default!;
        public SessionRootId RootId { get; }

        public string? TenantId { get; set; }
        public UserKey UserKey { get; set; }

        public int RotationCount { get; set; }
        public long SecurityVersionAtCreation { get; set; }

        public ClaimsSnapshot ClaimsSnapshot { get; set; } = ClaimsSnapshot.Empty;

        public AuthSessionId? ActiveSessionId { get; set; }

        public bool IsRevoked { get; set; }
        public DateTimeOffset? RevokedAt { get; set; }

        public byte[] RowVersion { get; set; } = default!;
    }
}
