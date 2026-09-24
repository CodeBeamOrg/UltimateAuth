namespace CodeBeam.UltimateAuth.Client;

/// <summary>
/// Specifies how an UltimateAuth authentication submission is executed.
/// </summary>
public enum UAuthSubmitMode
{
    /// <summary>
    /// Commits the authentication operation directly without first returning a structured try result to the caller.
    /// </summary>
    DirectCommit = 0,

    /// <summary>
    /// Attempts the authentication operation and returns its result without committing a successful authentication.
    /// </summary>
    TryOnly = 10,

    /// <summary>
    /// Attempts the authentication operation and, when successful, commits the authentication flow.
    /// </summary>
    TryAndCommit = 20,
}
