namespace CodeBeam.UltimateAuth.Server.Infrastructure;

/// <summary>
/// Public, deterministic keys for local development only.
/// Never use these values in production.
/// </summary>
public static class UAuthCryptographyConstants
{
    public const string DevelopmentJwtSigningKey =
        "VUxUSU1BVEVBVVRIX0RFVl9KV1RfU0lHTklOR19LRVlfVjE=";

    public const string DevelopmentTokenHashingKey =
        "VUxUSU1BVEVBVVRIX0RFVl9UT0tFTl9IQVNISU5HX0tFWV9WMQ==";

    public const string DefaultJwtKeyId = "uauth-hs256-v1";
}
