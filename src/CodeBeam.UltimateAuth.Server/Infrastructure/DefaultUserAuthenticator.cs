using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public sealed class DefaultUserAuthenticator<TUserId> : IUserAuthenticator<TUserId>
    {
        private readonly IUAuthUserStore<TUserId> _userStore;
        private readonly IUAuthPasswordHasher _passwordHasher;

        public DefaultUserAuthenticator(IUAuthUserStore<TUserId> userStore, IUAuthPasswordHasher passwordHasher)
        {
            _userStore = userStore;
            _passwordHasher = passwordHasher;
        }

        public async Task<UserAuthenticationResult<TUserId>> AuthenticateAsync(
            string? tenantId,
            string username,
            string secret,
            CancellationToken cancellationToken = default)
        {
            var user = await _userStore.FindByUsernameAsync(
                tenantId,
                username,
                cancellationToken);

            if (user is null)
                return UserAuthenticationResult<TUserId>.Fail();

            if (!user.IsActive)
                return UserAuthenticationResult<TUserId>.Fail();

            if (!_passwordHasher.Verify(secret, user.PasswordHash))
                return UserAuthenticationResult<TUserId>.Fail();

            return UserAuthenticationResult<TUserId>.Success(user.Id, user.Claims, user.RequiresMfa);
        }
    }
}
