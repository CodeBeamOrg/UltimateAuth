using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Server.Infrastructure
{
    public sealed class DefaultUserAuthenticator<TUserId> : IUserAuthenticator<TUserId>
    {
        private readonly IUAuthUserStore<TUserId> _userStore;
        private readonly IUAuthPasswordHasher _passwordHasher;

        public DefaultUserAuthenticator(
            IUAuthUserStore<TUserId> userStore,
            IUAuthPasswordHasher passwordHasher)
        {
            _userStore = userStore;
            _passwordHasher = passwordHasher;
        }

        public async Task<UserAuthenticationResult<TUserId>> AuthenticateAsync(
            string? tenantId,
            AuthenticationContext context,
            CancellationToken ct = default)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));

            if (!string.Equals(context.CredentialType, "password", StringComparison.Ordinal))
                return UserAuthenticationResult<TUserId>.Fail();

            var user = await _userStore.FindByUsernameAsync(
                tenantId,
                context.Identifier,
                ct);

            if (user is null || !user.IsActive)
                return UserAuthenticationResult<TUserId>.Fail();

            if (!_passwordHasher.Verify(context.Secret, user.PasswordHash))
                return UserAuthenticationResult<TUserId>.Fail();

            return UserAuthenticationResult<TUserId>.Success(
                user.Id,
                user.Claims,
                user.RequiresMfa);
        }
    }
}
