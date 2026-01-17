using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    public interface IAccessAuthority
    {
        AccessDecision Decide(AccessContext context);
    }
}
