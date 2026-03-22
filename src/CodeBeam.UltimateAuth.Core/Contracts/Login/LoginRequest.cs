using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed record LoginRequest
{
    public string Identifier { get; init; } = default!;
    public string Secret { get; init; } = default!;
    public CredentialType Factor { get; init; } = CredentialType.Password;
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    /// <summary>
    /// Hint to request access/refresh tokens when the server mode supports it.
    /// Server policy may still ignore this.
    /// </summary>
    public bool RequestTokens { get; init; } = true;
    public string? PreviewReceipt { get; init; }
}
