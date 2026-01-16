using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Credentials.InMemory
{
    internal sealed class UserKeyInMemoryUserIdProvider : IInMemoryUserIdProvider<UserKey>
    {
        public UserKey GetAdminUserId()
        {
            return UserKey.FromGuid(Guid.NewGuid());
        }
    }
}
