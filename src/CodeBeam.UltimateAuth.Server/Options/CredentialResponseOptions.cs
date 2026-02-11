using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Options;

namespace CodeBeam.UltimateAuth.Server.Options;

public sealed class CredentialResponseOptions
{
    public CredentialKind Kind { get; init; }
    public TokenResponseMode Mode { get; set; } = TokenResponseMode.None;

    /// <summary>
    /// Header or body name
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Applies when Mode = Header
    /// </summary>
    public HeaderTokenFormat HeaderFormat { get; set; } = HeaderTokenFormat.Bearer;
    public TokenFormat TokenFormat { get; set; }

    // Only for cookie
    public UAuthCookieOptions? Cookie { get; init; }

    internal CredentialResponseOptions Clone() => new()
    {
        Mode = Mode,
        Name = Name,
        HeaderFormat = HeaderFormat,
        TokenFormat = TokenFormat,
        Cookie = Cookie?.Clone()
    };

    public CredentialResponseOptions WithCookie(UAuthCookieOptions cookie)
    {
        if (Mode != TokenResponseMode.Cookie)
            throw new InvalidOperationException("Cookie can only be set when Mode = Cookie.");

        return new CredentialResponseOptions()
        {
            Kind = Kind,
            Mode = Mode,
            Name = Name,
            HeaderFormat = HeaderFormat,
            TokenFormat = TokenFormat,
            Cookie = cookie
        };
    }

    public static CredentialResponseOptions Disabled(CredentialKind kind)
        => new()
        {
            Kind = kind,
            Mode = TokenResponseMode.None
        };
}
