using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Server.Options;

// TODO(security):
// Validate identifier uniqueness against login identifier configuration.
// Identifier types used for direct login resolution should normally be tenant-unique unless a custom resolver explicitly guarantees
// unambiguous user resolution.
public sealed class UAuthIdentifierUniquenessOptions
{
    public UniquenessScope Username { get; set; } = UniquenessScope.Tenant;
    public UniquenessScope Email { get; set; } = UniquenessScope.Tenant;
    public UniquenessScope Phone { get; set; } = UniquenessScope.Tenant;
    public UniquenessScope Custom { get; set; } = UniquenessScope.Tenant;

    internal UAuthIdentifierUniquenessOptions Clone() => new()
    {
        Username = Username,
        Email = Email,
        Phone = Phone,
        Custom = Custom
    };
}
