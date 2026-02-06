namespace CodeBeam.UltimateAuth.Core.Abstractions;

/// <summary>
/// Generates cryptographically secure random tokens
/// for opaque identifiers, refresh tokens, session ids.
/// </summary>
public interface IOpaqueTokenGenerator
{
    string Generate();
    string GenerateJwtId();
}
