using CodeBeam.UltimateAuth.Client.Contracts;

namespace CodeBeam.UltimateAuth.Client.Options;

public sealed class UAuthClientMultiTenantOptions
{
    /// <summary>
    /// Enables tenant propagation from client to server.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Tenant identifier to propagate.
    /// Client does NOT resolve tenant, only carries it.
    /// </summary>
    public string? Tenant { get; set; }

    /// <summary>
    /// Transport mechanism for tenant propagation.
    /// </summary>
    public TenantTransport Transport { get; set; } = TenantTransport.None;
}
