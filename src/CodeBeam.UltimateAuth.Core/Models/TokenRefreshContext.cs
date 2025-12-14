namespace CodeBeam.UltimateAuth.Core.Models
{
    public sealed record TokenRefreshContext
    {
        public string? TenantId { get; init; }

        public string RefreshToken { get; init; } = default!;
    }
}
