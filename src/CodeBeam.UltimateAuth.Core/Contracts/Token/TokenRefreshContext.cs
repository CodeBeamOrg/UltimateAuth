namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed record TokenRefreshContext
    {
        public string? TenantId { get; init; }

        public string RefreshToken { get; init; } = default!;
    }
}
