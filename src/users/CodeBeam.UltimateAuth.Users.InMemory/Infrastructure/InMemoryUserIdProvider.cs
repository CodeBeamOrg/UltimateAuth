using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Infrastructure;

namespace CodeBeam.UltimateAuth.Users.InMemory
{
    public sealed class InMemoryUserIdProvider : IInMemoryUserIdProvider<UserKey>
    {
        public UserKey GetAdminUserId() => UserKey.FromString("admin");
        public UserKey GetUserUserId() => UserKey.FromString("user");
    }
}
