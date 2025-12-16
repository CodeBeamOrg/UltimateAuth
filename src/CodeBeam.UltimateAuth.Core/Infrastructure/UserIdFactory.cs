using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Infrastructure
{
    public sealed class UserIdFactory : IUserIdFactory<UserId>
    {
        public UserId Create() => new UserId(Guid.NewGuid().ToString("N"));
    }
}
