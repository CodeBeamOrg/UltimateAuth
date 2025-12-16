using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Core.Contracts
{
    public sealed class UserAuthenticationResult<TUserId>
    {
        public bool Succeeded { get; init; }

        public TUserId? UserId { get; init; }

        public ClaimsSnapshot? Claims { get; init; }

        public bool RequiresMfa { get; init; }

        public static UserAuthenticationResult<TUserId> Fail() => new() { Succeeded = false };

        public static UserAuthenticationResult<TUserId> Success(TUserId userId, ClaimsSnapshot claims, bool requiresMfa = false)
            => new()
            {
                Succeeded = true,
                UserId = userId,
                Claims = claims,
                RequiresMfa = requiresMfa
            };
    }
}
