using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.IdentityLab.Services;

public interface ILabRootResolver
{
    bool IsLadmin(AccessContext context);
}
