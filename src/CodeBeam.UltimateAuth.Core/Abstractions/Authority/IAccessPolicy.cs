using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Core.Abstractions
{
    public interface IAccessPolicy
    {
        bool AppliesTo(AccessContext context);
        AccessDecision Decide(AccessContext context);
    }
}
