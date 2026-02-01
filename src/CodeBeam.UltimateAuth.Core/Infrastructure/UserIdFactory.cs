using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Infrastructure;

public sealed class UserIdFactory : IUserIdFactory<UserKey>
{
    public UserKey Create() => UserKey.New();
}
