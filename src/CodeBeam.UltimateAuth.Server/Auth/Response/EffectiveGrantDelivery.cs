using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Options;

namespace CodeBeam.UltimateAuth.Server.Auth;

public sealed class EffectiveGrantDelivery
{
    public GrantKind Kind { get; }
    public TokenFormat TokenFormat { get; }
    public GrantDeliveryMode Mode { get; }
    public string? Name { get; }
    public HeaderTokenFormat HeaderFormat { get; }
    public UAuthCookieOptions? Cookie { get; }

    public EffectiveGrantDelivery(
        GrantKind kind,
        TokenFormat tokenFormat,
        GrantDeliveryMode mode,
        string? name = null,
        HeaderTokenFormat headerFormat = HeaderTokenFormat.Bearer,
        UAuthCookieOptions? cookie = null)
    {
        Kind = kind;
        TokenFormat = tokenFormat;
        Mode = mode;
        Name = name;
        HeaderFormat = headerFormat;
        Cookie = cookie;
    }

    public static EffectiveGrantDelivery Disabled(GrantKind kind)
        => new(
            kind,
            TokenFormat.Opaque,
            GrantDeliveryMode.None);
}
