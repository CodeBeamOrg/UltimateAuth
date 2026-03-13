using CodeBeam.UltimateAuth.Client.Services;

namespace CodeBeam.UltimateAuth.Client;

public interface IUAuthClient
{
    IFlowClient Flows { get; }
    ISessionClient Sessions { get; }
    IUserClient Users { get; }
    IUserIdentifierClient Identifiers { get; }
    ICredentialClient Credentials { get; }
    IAuthorizationClient Authorization { get; }
}
