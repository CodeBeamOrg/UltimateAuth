using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Auth;

public interface IPrimaryTokenResolver
{
    PrimaryTokenKind Resolve(UAuthMode effectiveMode);
}
