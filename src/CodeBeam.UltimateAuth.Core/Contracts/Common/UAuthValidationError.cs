namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed record UAuthValidationError(
    string Code,
    string? Field = null);