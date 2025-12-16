using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    public interface IAuthAuthority
    {
        AuthorizationResult Decide(AuthContext context);
    }

}
