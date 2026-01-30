using CodeBeam.UltimateAuth.Client.Services;

namespace CodeBeam.UltimateAuth.Client;

internal sealed class UAuthClient : IUAuthClient
{
    public IFlowClient Flows { get; }
    public IUserClient Users { get; }
    public IUserIdentifierClient Identifiers { get; }

    public UAuthClient(IFlowClient flows, IUserClient users, IUserIdentifierClient identifiers)
    {
        Flows = flows;
        Users = users;
        Identifiers = identifiers;
    }
}
