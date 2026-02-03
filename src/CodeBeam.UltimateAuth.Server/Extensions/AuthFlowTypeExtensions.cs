using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Server.Extensions;

public static class AuthFlowTypeExtensions
{
    public static AuthOperation ToAuthOperation(this AuthFlowType flowType)
        => flowType switch
        {
            AuthFlowType.Login => AuthOperation.Login,
            AuthFlowType.Reauthentication => AuthOperation.Login,

            AuthFlowType.ApiAccess => AuthOperation.Access,
            AuthFlowType.ValidateSession => AuthOperation.Access,
            AuthFlowType.UserInfo => AuthOperation.Access,
            AuthFlowType.PermissionQuery => AuthOperation.Access,
            AuthFlowType.IssueToken => AuthOperation.Access,
            AuthFlowType.IntrospectToken => AuthOperation.Access,

            AuthFlowType.RefreshSession => AuthOperation.Refresh,
            AuthFlowType.RefreshToken => AuthOperation.Refresh,

            AuthFlowType.Logout => AuthOperation.Logout,
            AuthFlowType.RevokeSession => AuthOperation.Revoke,
            AuthFlowType.RevokeToken => AuthOperation.Revoke,

            AuthFlowType.QuerySession => AuthOperation.System,

            _ => throw new InvalidOperationException($"Unsupported flow type: {flowType}")
        };
}
