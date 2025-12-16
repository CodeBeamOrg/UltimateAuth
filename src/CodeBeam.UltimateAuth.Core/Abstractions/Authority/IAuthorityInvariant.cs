using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    public interface IAuthorityInvariant
    {
        AuthorizationResult Decide(AuthContext context);
    }
}
