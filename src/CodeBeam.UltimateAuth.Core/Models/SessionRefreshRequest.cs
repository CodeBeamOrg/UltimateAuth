namespace CodeBeam.UltimateAuth.Core.Models
{
    public sealed record SessionRefreshRequest
    {
        public string? TenantId { get; init; }
        public string RefreshToken { get; init; } = default!;
    }
}
