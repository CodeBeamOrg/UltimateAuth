using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Infrastructure;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using CodeBeam.UltimateAuth.Tokens.InMemory;
using System.Text;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public sealed class RefreshTokenValidatorTests
{
    private const string ValidDeviceId = "deviceidshouldbelongandstrongenough!?1234567890";

    private static UAuthRefreshTokenValidator CreateValidator(InMemoryRefreshTokenStore store)
    {
        return new UAuthRefreshTokenValidator(store, CreateHasher());
    }

    private static ITokenHasher CreateHasher()
    {
        return new HmacSha256TokenHasher(Encoding.UTF8.GetBytes("unit-test-secret-key"));
    }

    [Fact]
    public async Task Invalid_When_Token_Not_Found()
    {
        var store = new InMemoryRefreshTokenStore();
        var validator = CreateValidator(store);

        var result = await validator.ValidateAsync(
            new RefreshTokenValidationContext
            {
                Tenant = TenantKey.Single,
                RefreshToken = "non-existing",
                Now = DateTimeOffset.UtcNow,
                Device = DeviceContext.Create(DeviceId.Create(ValidDeviceId), null, null, null, null, null),
            });

        Assert.False(result.IsValid);
        Assert.False(result.IsReuseDetected);
    }

    [Fact]
    public async Task Reuse_Detected_When_Token_is_Revoked()
    {
        var store = new InMemoryRefreshTokenStore();
        var hasher = CreateHasher();
        var validator = CreateValidator(store);

        var now = DateTimeOffset.UtcNow;

        var rawToken = "refresh-token-1";
        var hash = hasher.Hash(rawToken);

        await store.StoreAsync(TenantKey.Single, new StoredRefreshToken
        {
            Tenant = TenantKey.Single,
            TokenHash = hash,
            UserKey = UserKey.FromString("user-1"),
            SessionId = TestIds.Session("session-1-aaaaaaaaaaaaaaaaaaaaaa"),
            ChainId = SessionChainId.New(),
            IssuedAt = now.AddMinutes(-5),
            ExpiresAt = now.AddMinutes(5),
            RevokedAt = now
        });

        var result = await validator.ValidateAsync(
            new RefreshTokenValidationContext
            {
                Tenant = TenantKey.Single,
                RefreshToken = rawToken,
                Now = now,
                Device = DeviceContext.Create(DeviceId.Create(ValidDeviceId), null, null, null, null, null),
            });

        Assert.False(result.IsValid);
        Assert.True(result.IsReuseDetected);
    }

    [Fact]
    public async Task Invalid_When_Expected_Session_Id_Does_Not_Match()
    {
        var store = new InMemoryRefreshTokenStore();
        var validator = CreateValidator(store);

        var now = DateTimeOffset.UtcNow;

        await store.StoreAsync(TenantKey.Single, new StoredRefreshToken
        {
            Tenant = TenantKey.Single,
            TokenHash = "hash-2",
            UserKey = UserKey.FromString("user-1"),
            SessionId = TestIds.Session("session-1-bbbbbbbbbbbbbbbbbbbbbb"),
            ChainId = SessionChainId.New(),
            IssuedAt = now,
            ExpiresAt = now.AddMinutes(10)
        });

        var result = await validator.ValidateAsync(
            new RefreshTokenValidationContext
            {
                Tenant = TenantKey.Single,
                RefreshToken = "hash-2",
                ExpectedSessionId = TestIds.Session("session-2-cccccccccccccccccccccc"),
                Now = now,
                Device = DeviceContext.Create(DeviceId.Create(ValidDeviceId), null, null, null, null, null),
            });

        Assert.False(result.IsValid);
        Assert.False(result.IsReuseDetected);
    }

}

