namespace CodeBeam.UltimateAuth.Core.Contracts;

// TODO: It's same as TokenType
// It's not primary token kind, it's about transport format.
public enum TokenFormat
{
    Opaque = 0,
    Jwt = 10
}
