using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Credentials.EntityFrameworkCore
{
    internal sealed class EfCoreAuthUser<TUserId> : IUser<TUserId>
    {
        public TUserId UserId { get; }

        IReadOnlyDictionary<string, object>? IUser<TUserId>.Claims => null;

        public EfCoreAuthUser(TUserId userId)
        {
            UserId = userId;
        }
    }
}
