namespace CodeBeam.UltimateAuth.Client;

public enum UAuthStateEvent
{
    ValidationCalled,
    IdentifiersChanged,
    ProfileChanged,
    CredentialsChanged,
    CredentialsChangedSelf,
    RolesChanged,
    PermissionsChanged,
    SessionRevoked,
    UserDeleted
}
