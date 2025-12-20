namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed record AuthenticationContext
    {
        public string? TenantId { get; init; }
        public string Identifier { get; init; } = default!;
        public string Secret { get; init; } = default!;
        public AuthOperation Operation { get; init; } // Login, Reauth, Validate
        public DeviceContext? Device { get; init; }
        public string CredentialType { get; init; } = "password";
    }
}
