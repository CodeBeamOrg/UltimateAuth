using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts;

public sealed record PrimaryToken
{
    public PrimaryTokenKind Kind { get; }
    public string Value { get; }

    private PrimaryToken(PrimaryTokenKind kind, string value)
    {
        Kind = kind;
        Value = value;
    }

    public static PrimaryToken FromSession(AuthSessionId sessionId) => new(PrimaryTokenKind.Session, sessionId.ToString());

    public static PrimaryToken FromAccessToken(AccessToken token) => new(PrimaryTokenKind.AccessToken, token.Token);
}
