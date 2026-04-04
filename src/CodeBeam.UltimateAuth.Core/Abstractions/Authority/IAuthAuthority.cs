using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Core.Abstractions;

public interface IAuthAuthority
{
    AccessDecisionResult Decide(AuthContext context, IEnumerable<IAuthorityPolicy>? policies = null);
}
