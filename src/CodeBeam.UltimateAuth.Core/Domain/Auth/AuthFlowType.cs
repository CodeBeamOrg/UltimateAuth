namespace CodeBeam.UltimateAuth.Core.Domain;

public enum AuthFlowType
{
    Login = 0,
    Reauthentication = 10,
    Logout = 20,

    RefreshSession = 100,
    ValidateSession = 110,
    QuerySession = 120,
    RevokeSession = 130,

    IssueToken = 200,
    RefreshToken = 210,
    IntrospectToken = 220,
    RevokeToken = 230,

    UserInfo = 300,
    PermissionQuery = 310,

    UserManagement = 400,
    UserProfileManagement = 410,
    UserIdentifierManagement = 420,
    CredentialManagement = 430,
    AuthorizationManagement = 440,

    ApiAccess = 500
}
