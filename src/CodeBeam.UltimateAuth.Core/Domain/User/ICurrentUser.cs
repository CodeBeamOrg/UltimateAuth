using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    UserKey UserKey { get; }
}
