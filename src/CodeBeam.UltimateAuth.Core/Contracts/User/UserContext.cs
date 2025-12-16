using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed class UserContext<TUserId>
    {
        public TUserId? UserId { get; init; }
        public IUser<TUserId>? User { get; init; }

        public bool IsAuthenticated => UserId is not null;

        public static UserContext<TUserId> Anonymous() => new();
    }
}
