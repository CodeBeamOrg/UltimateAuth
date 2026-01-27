using CodeBeam.UltimateAuth.Client.Services;

namespace CodeBeam.UltimateAuth.Client
{
    public interface IUAuthClient
    {
        IFlowClient Flows { get; }
        IUserClient Users { get; }
    }
}
