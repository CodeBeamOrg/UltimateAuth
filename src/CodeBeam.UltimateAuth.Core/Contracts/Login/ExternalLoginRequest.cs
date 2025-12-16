namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed record ExternalLoginRequest
    {
        public string? TenantId { get; init; }
        public string Provider { get; init; } = default!;
        public string ExternalToken { get; init; } = default!;
        public string? DeviceId { get; init; }
    }

}
