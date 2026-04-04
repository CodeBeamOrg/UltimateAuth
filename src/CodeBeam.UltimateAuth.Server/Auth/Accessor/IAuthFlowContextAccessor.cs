namespace CodeBeam.UltimateAuth.Server.Auth;

public interface IAuthFlowContextAccessor
{
    AuthFlowContext Current { get; }
}
