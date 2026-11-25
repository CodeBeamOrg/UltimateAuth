namespace CodeBeam.UltimateAuth.Core.Options;

[Obsolete("TokenResponseMode is no longer supported. Use GrantDeliveryMode instead.")]
public enum TokenResponseMode
{
    None = 0,
    Cookie = 10,
    Header = 20,
    Body = 30
}
