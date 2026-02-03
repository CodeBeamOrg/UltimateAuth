using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Credentials.Contracts;

public sealed record ResetPasswordRequest
{
    public UserKey UserKey { get; init; } = default!;
    public required string NewPassword { get; init; }

    /// <summary>
    /// Optional reset token or verification code.
    /// </summary>
    public string? Token { get; init; }
}
