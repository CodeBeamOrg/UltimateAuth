namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed record AuthContext
    {
        public string? TenantId { get; init; }

        public AuthOperation Operation { get; init; }

        public UAuthMode Mode { get; init; }

        public SessionAccessContext? Session { get; init; }

        public DeviceContext Device { get; init; }

        public DateTimeOffset Now { get; init; }
    }
}
