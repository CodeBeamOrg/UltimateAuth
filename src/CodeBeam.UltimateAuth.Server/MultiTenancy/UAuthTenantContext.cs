namespace CodeBeam.UltimateAuth.Server.MultiTenancy
{
    public class UAuthTenantContext
    {
        public string? TenantId { get; set; }
        public bool IsResolved { get; set; }
    }
}
