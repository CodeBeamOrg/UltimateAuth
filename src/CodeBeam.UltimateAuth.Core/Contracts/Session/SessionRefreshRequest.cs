namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed record SessionRefreshRequest
    {
        public string? TenantId { get; init; }
        public string RefreshToken { get; init; } = default!;
    }
}
