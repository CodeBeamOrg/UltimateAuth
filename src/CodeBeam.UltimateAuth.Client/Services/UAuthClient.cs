using CodeBeam.UltimateAuth.Client.Services;

namespace CodeBeam.UltimateAuth.Client;

internal sealed class UAuthClient : IUAuthClient
{
    public IFlowClient Flows { get; }
    public IUserClient Users { get; }
    public IUserIdentifierClient Identifiers { get; }
    public IAuthorizationClient Authorization { get; }

    public UAuthClient(IFlowClient flows, IUserClient users, IUserIdentifierClient identifiers, IAuthorizationClient authorization)
    {
        Flows = flows;
        Users = users;
        Identifiers = identifiers;
        Authorization = authorization;
    }
}
