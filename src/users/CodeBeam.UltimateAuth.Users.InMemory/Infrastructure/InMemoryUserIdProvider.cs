using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Infrastructure;

namespace CodeBeam.UltimateAuth.Users.InMemory
{
    public sealed class InMemoryUserIdProvider : IInMemoryUserIdProvider<UserKey>
    {
        private static readonly UserKey Admin = UserKey.FromString("admin");
        private static readonly UserKey User = UserKey.FromString("user");

        public UserKey GetAdminUserId() => Admin;
        public UserKey GetUserUserId() => User;
    }

}
