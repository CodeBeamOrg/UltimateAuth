using CodeBeam.UltimateAuth.Client.Services;

namespace CodeBeam.UltimateAuth.Client;

internal sealed class UAuthClient : IUAuthClient
{
    public IFlowClient Flows { get; }
    public ISessionClient Sessions { get; }
    public IUserClient Users { get; }
    public IUserIdentifierClient Identifiers { get; }
    public ICredentialClient Credentials { get; }
    public IAuthorizationClient Authorization { get; }

    public UAuthClient(IFlowClient flows, ISessionClient session, IUserClient users, IUserIdentifierClient identifiers, ICredentialClient credentials, IAuthorizationClient authorization)
    {
        Flows = flows;
        Sessions = session;
        Users = users;
        Identifiers = identifiers;
        Credentials = credentials;
        Authorization = authorization;
    }
}
