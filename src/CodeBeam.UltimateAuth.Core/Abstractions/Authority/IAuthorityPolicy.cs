using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    public interface IAuthorityPolicy
    {
        bool AppliesTo(AuthContext context);
        AuthorizationResult Decide(AuthContext context);
    }
}
