namespace CodeBeam.UltimateAuth.Client;

public enum UAuthStateEvent
{
    ValidationCalled,
    IdentifiersChanged,
    UserStatusChanged,
    ProfileChanged,
    CredentialsChanged,
    CredentialsChangedSelf,
    AuthorizationChanged,
    SessionRevoked,
    UserDeleted,
    LogoutVariant
}
