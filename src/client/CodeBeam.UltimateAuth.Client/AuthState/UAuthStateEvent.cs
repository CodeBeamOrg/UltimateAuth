namespace CodeBeam.UltimateAuth.Client;

/// <summary>
/// Identifies events that may affect the UltimateAuth client authentication state.
/// </summary>
public enum UAuthStateEvent
{
    /// <summary>
    /// Indicates that authentication state validation was requested.
    /// </summary>
    ValidationCalled,

    /// <summary>
    /// Indicates that one or more identifiers associated with the user changed.
    /// </summary>
    IdentifiersChanged,

    /// <summary>
    /// Indicates that the user's status changed.
    /// </summary>
    UserStatusChanged,

    /// <summary>
    /// Indicates that the user's profile information changed.
    /// </summary>
    ProfileChanged,

    /// <summary>
    /// Indicates that credentials associated with a user changed.
    /// </summary>
    CredentialsChanged,

    /// <summary>
    /// Indicates that the current user's own credentials changed.
    /// </summary>
    CredentialsChangedSelf,

    /// <summary>
    /// Indicates that authorization information associated with the user changed.
    /// </summary>
    AuthorizationChanged,

    /// <summary>
    /// Indicates that a session associated with the user was revoked.
    /// </summary>
    SessionRevoked,

    /// <summary>
    /// Indicates that the user was deleted.
    /// </summary>
    UserDeleted,

    /// <summary>
    /// Indicates that a logout operation affecting the authentication state occurred.
    /// </summary>
    LogoutVariant
}
