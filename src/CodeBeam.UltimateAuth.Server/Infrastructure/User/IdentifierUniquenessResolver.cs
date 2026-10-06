using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Users.Contracts;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

// TODO(policy): Move identifier uniqueness decision/enforcement to the Policy layer.
public static class IdentifierUniquenessResolver
{
    public static UniquenessScope GetScope(UAuthServerOptions options, UserIdentifierType type)
    {
        ArgumentNullException.ThrowIfNull(options);

        var uniqueness = options.Identifiers.Uniqueness;

        return type switch
        {
            UserIdentifierType.Username => uniqueness.Username,
            UserIdentifierType.Email => uniqueness.Email,
            UserIdentifierType.Phone => uniqueness.Phone,
            _ => uniqueness.Custom
        };
    }
}
