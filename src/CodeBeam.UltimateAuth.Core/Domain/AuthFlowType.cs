namespace CodeBeam.UltimateAuth.Core.Domain;

public enum AuthFlowType
{
    Login,
    Reauthentication,

    Logout,
    RefreshSession,
    ValidateSession,

    IssueToken,
    RefreshToken,
    IntrospectToken,
    RevokeToken,

    QuerySession,
    RevokeSession,

    UserInfo,
    PermissionQuery,

    UserManagement,
    UserProfileManagement,
    UserIdentifierManagement,
    CredentialManagement,
    AuthorizationManagement,

    ApiAccess
}
