namespace CodeBeam.UltimateAuth.Client;

public enum UAuthStateEvent
{
    ValidationCalled,
    IdentifiersChanged,
    UserStatusChanged,
    ProfileChanged,
    CredentialsChanged,
    CredentialsChangedSelf,
    RolesChanged,
    PermissionsChanged,
    SessionRevoked,
    UserDeleted,
    LogoutVariant
}
