using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    public interface IAccessInvariant
    {
        AccessDecision Decide(AccessContext context);
    }
}
