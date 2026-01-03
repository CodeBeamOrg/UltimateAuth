namespace CodeBeam.UltimateAuth.Server.Auth
{
    public interface IAuthResponseResolver
    {
        EffectiveAuthResponse Resolve(AuthFlowContext context);
    }
}
