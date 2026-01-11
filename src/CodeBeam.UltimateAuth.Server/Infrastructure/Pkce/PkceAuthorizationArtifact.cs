using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Stores;

namespace CodeBeam.UltimateAuth.Server.Infrastructure;

/// <summary>
/// Represents a PKCE authorization process that has been initiated
/// but not yet completed. This artifact is short-lived, single-use,
/// and must be consumed atomically.
/// </summary>
public sealed class PkceAuthorizationArtifact : AuthArtifact
{
    public PkceAuthorizationArtifact(
        AuthArtifactKey authorizationCode,
        string codeChallenge,
        PkceChallengeMethod challengeMethod,
        DateTimeOffset expiresAt,
        int maxAttempts,
        PkceContextSnapshot context)
        : base(AuthArtifactType.PkceAuthorizationCode, expiresAt, maxAttempts)
    {
        AuthorizationCode = authorizationCode;
        CodeChallenge = codeChallenge;
        ChallengeMethod = challengeMethod;
        Context = context;
    }

    /// <summary>
    /// Opaque authorization code issued to the client.
    /// This is the lookup key in the AuthStore.
    /// </summary>
    public AuthArtifactKey AuthorizationCode { get; }

    /// <summary>
    /// Base64Url-encoded hashed code challenge (S256).
    /// The original verifier is never stored.
    /// </summary>
    public string CodeChallenge { get; }

    public PkceChallengeMethod ChallengeMethod { get; }

    /// <summary>
    /// Immutable snapshot of client and request context
    /// at the time the PKCE flow was initiated.
    /// </summary>
    public PkceContextSnapshot Context { get; }
}
