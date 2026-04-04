namespace CodeBeam.UltimateAuth.Server.Infrastructure;

public readonly record struct NormalizedIdentifier(
    string Raw,
    string Normalized,
    bool IsValid,
    string? ErrorCode);
