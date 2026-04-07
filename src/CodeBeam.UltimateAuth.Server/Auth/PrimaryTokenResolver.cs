using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Auth;

internal sealed class PrimaryTokenResolver : IPrimaryTokenResolver
{
    public PrimaryTokenKind Resolve(UAuthMode effectiveMode)
    {
        return effectiveMode switch
        {
            UAuthMode.PureOpaque => PrimaryTokenKind.Session,
            UAuthMode.Hybrid => PrimaryTokenKind.Session,
            UAuthMode.SemiHybrid => PrimaryTokenKind.AccessToken,
            UAuthMode.PureJwt => PrimaryTokenKind.AccessToken,
            _ => throw new InvalidOperationException(
                $"Unsupported auth mode: {effectiveMode}")
        };
    }
}
