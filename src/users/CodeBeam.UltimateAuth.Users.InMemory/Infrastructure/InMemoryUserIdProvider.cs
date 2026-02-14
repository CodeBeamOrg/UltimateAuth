using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Infrastructure;

namespace CodeBeam.UltimateAuth.Users.InMemory;

public sealed class InMemoryUserIdProvider : IInMemoryUserIdProvider<UserKey>
{
    private static readonly UserKey Admin = UserKey.FromGuid(Guid.NewGuid());
    private static readonly UserKey User = UserKey.FromGuid(Guid.NewGuid());

    public UserKey GetAdminUserId() => Admin;
    public UserKey GetUserUserId() => User;
}
