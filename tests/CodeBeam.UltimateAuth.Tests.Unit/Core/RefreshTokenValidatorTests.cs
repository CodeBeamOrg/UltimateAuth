using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Infrastructure;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Tokens.InMemory;
using System.Text;

namespace CodeBeam.UltimateAuth.Tests.Unit.Core
{
    public sealed class RefreshTokenValidatorTests
    {
        private static DefaultRefreshTokenValidator<string> CreateValidator(InMemoryRefreshTokenStore<string> store)
        {
            return new DefaultRefreshTokenValidator<string>(store, CreateHasher());
        }

        private static ITokenHasher CreateHasher()
        {
            return new HmacSha256TokenHasher(Encoding.UTF8.GetBytes("unit-test-secret-key"));
        }

        [Fact]
        public async Task Invalid_When_Token_Not_Found()
        {
            var store = new InMemoryRefreshTokenStore<string>();
            var validator = CreateValidator(store);

            var result = await validator.ValidateAsync(
                new RefreshTokenValidationContext<string>
                {
                    TenantId = null,
                    RefreshToken = "non-existing",
                    Now = DateTimeOffset.UtcNow
                });

            Assert.False(result.IsValid);
            Assert.False(result.IsReuseDetected);
        }

        [Fact]
        public async Task Reuse_Detected_When_Token_is_Revoked()
        {
            var store = new InMemoryRefreshTokenStore<string>();
            var hasher = CreateHasher();
            var validator = CreateValidator(store);

            var now = DateTimeOffset.UtcNow;

            var rawToken = "refresh-token-1";
            var hash = hasher.Hash(rawToken);

            await store.StoreAsync(null, new StoredRefreshToken<string>
            {
                TenantId = null,
                TokenHash = hash,
                UserId = "user-1",
                SessionId = new AuthSessionId("s1"),
                ChainId = ChainId.New(),
                IssuedAt = now.AddMinutes(-5),
                ExpiresAt = now.AddMinutes(5),
                RevokedAt = now
            });

            var result = await validator.ValidateAsync(
                new RefreshTokenValidationContext<string>
                {
                    TenantId = null,
                    RefreshToken = rawToken,
                    Now = now
                });

            Assert.False(result.IsValid);
            Assert.True(result.IsReuseDetected);
        }

        [Fact]
        public async Task Invalid_When_Expected_Session_Id_Does_Not_Match()
        {
            var store = new InMemoryRefreshTokenStore<string>();
            var validator = CreateValidator(store);

            var now = DateTimeOffset.UtcNow;

            await store.StoreAsync(null, new StoredRefreshToken<string>
            {
                TenantId = null,
                TokenHash = "hash-2",
                UserId = "user-1",
                SessionId = new AuthSessionId("s1"),
                ChainId = ChainId.New(),
                IssuedAt = now,
                ExpiresAt = now.AddMinutes(10)
            });

            var result = await validator.ValidateAsync(
                new RefreshTokenValidationContext<string>
                {
                    TenantId = null,
                    RefreshToken = "hash-2",
                    ExpectedSessionId = new AuthSessionId("s2"),
                    Now = now
                });

            Assert.False(result.IsValid);
            Assert.False(result.IsReuseDetected);
        }

    }
}

