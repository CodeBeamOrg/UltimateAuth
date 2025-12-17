namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed record AuthContext
    {
        public string? TenantId { get; init; }

        public AuthOperation Operation { get; init; }

        public UAuthMode Mode { get; init; }

        public SessionAccessContext? Session { get; init; }

        public DeviceContext Device { get; init; }

        public DateTimeOffset At { get; init; }

        private AuthContext() { }

        public static AuthContext System(string? tenantId, AuthOperation operation, DateTimeOffset at, UAuthMode mode = UAuthMode.Hybrid)
        {
            return new AuthContext
            {
                TenantId = tenantId,
                Operation = operation,
                Mode = mode,
                At = at,
                Session = null,
                Device = null
            };
        }

        public static AuthContext ForAuthenticatedUser(string? tenantId, AuthOperation operation, DateTimeOffset at, DeviceContext device, UAuthMode mode = UAuthMode.Hybrid)
        {
            return new AuthContext
            {
                TenantId = tenantId,
                Operation = operation,
                Mode = mode,
                At = at,
                Device = device,
                Session = null
            };
        }

        public static AuthContext ForSession(string? tenantId, AuthOperation operation, SessionAccessContext session, DateTimeOffset at,
            DeviceContext device, UAuthMode mode = UAuthMode.Hybrid)
        {
            return new AuthContext
            {
                TenantId = tenantId,
                Operation = operation,
                Mode = mode,
                At = at,
                Session = session,
                Device = device
            };
        }


    }
}
