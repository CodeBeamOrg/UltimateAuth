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

    private static UAuthRefreshTokenValidator CreateValidator(InMemoryRefreshTokenStoreFactory factory)
    {
        return new UAuthRefreshTokenValidator(factory, CreateHasher());
    }

    private static ITokenHasher CreateHasher()
    {
        return new HmacSha256TokenHasher(Encoding.UTF8.GetBytes("unit-test-secret-key"));
    }

    [Fact]
    public async Task Invalid_When_Token_Not_Found()
    {
        var factory = new InMemoryRefreshTokenStoreFactory();
        var validator = CreateValidator(factory);

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
        var factory = new InMemoryRefreshTokenStoreFactory();
        var store = factory.Create(TenantKey.Single);

        var hasher = CreateHasher();
        var validator = CreateValidator(factory);

        var now = DateTimeOffset.UtcNow;

        var rawToken = "refresh-token-1";
        var hash = hasher.Hash(rawToken);

        var token = RefreshToken.Create(
            TokenId.New(),
            hash,
            TenantKey.Single,
            UserKey.FromString("user-1"),
            TestIds.Session("session-1-aaaaaaaaaaaaaaaaaaaaaa"),
            SessionChainId.New(),
            now.AddMinutes(-5),
            now.AddMinutes(5));

        var revoked = token.Revoke(now);

        await store.StoreAsync(revoked);

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
        var factory = new InMemoryRefreshTokenStoreFactory();
        var store = factory.Create(TenantKey.Single);

        var validator = CreateValidator(factory);

        var now = DateTimeOffset.UtcNow;

        var token = RefreshToken.Create(
            TokenId.New(),
            "hash-2",
            TenantKey.Single,
            UserKey.FromString("user-1"),
            TestIds.Session("session-1-bbbbbbbbbbbbbbbbbbbbbb"),
            SessionChainId.New(),
            now,
            now.AddMinutes(10));

        await store.StoreAsync(token);

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

    [Fact]
    public async Task Invalid_When_Token_Is_Expired()
    {
        var factory = new InMemoryRefreshTokenStoreFactory();
        var store = factory.Create(TenantKey.Single);

        var validator = CreateValidator(factory);

        var now = DateTimeOffset.UtcNow;

        var token = RefreshToken.Create(
            TokenId.New(),
            "expired-hash",
            TenantKey.Single,
            UserKey.FromString("user-1"),
            TestIds.Session("session-expired"),
            SessionChainId.New(),
            now.AddMinutes(-10),
            now.AddMinutes(-1));

        await store.StoreAsync(token);

        var result = await validator.ValidateAsync(
            new RefreshTokenValidationContext
            {
                Tenant = TenantKey.Single,
                RefreshToken = "expired-hash",
                Now = now,
                Device = DeviceContext.Create(DeviceId.Create(ValidDeviceId), null, null, null, null, null),
            });

        Assert.False(result.IsValid);
        Assert.False(result.IsReuseDetected);
    }

    [Fact]
    public async Task Valid_When_Token_Is_Active()
    {
        var factory = new InMemoryRefreshTokenStoreFactory();
        var store = factory.Create(TenantKey.Single);

        var validator = CreateValidator(factory);

        var now = DateTimeOffset.UtcNow;

        var raw = "valid-token";
        var hash = CreateHasher().Hash(raw);

        var token = RefreshToken.Create(
            TokenId.New(),
            hash,
            TenantKey.Single,
            UserKey.FromString("user-1"),
            TestIds.Session("session-valid"),
            SessionChainId.New(),
            now,
            now.AddMinutes(10));

        await store.StoreAsync(token);

        var result = await validator.ValidateAsync(
            new RefreshTokenValidationContext
            {
                Tenant = TenantKey.Single,
                RefreshToken = raw,
                Now = now,
                Device = DeviceContext.Create(DeviceId.Create(ValidDeviceId), null, null, null, null, null),
            });

        Assert.True(result.IsValid);
        Assert.False(result.IsReuseDetected);
    }

    [Fact]
    public async Task Reuse_Detected_When_Old_Token_Is_Reused_After_Rotation()
    {
        var factory = new InMemoryRefreshTokenStoreFactory();
        var store = factory.Create(TenantKey.Single);

        var validator = CreateValidator(factory);

        var now = DateTimeOffset.UtcNow;

        var raw = "token-1";
        var hash = CreateHasher().Hash(raw);

        var token = RefreshToken.Create(
            TokenId.New(),
            hash,
            TenantKey.Single,
            UserKey.FromString("user-1"),
            TestIds.Session("session-rotate"),
            SessionChainId.New(),
            now,
            now.AddMinutes(10));

        await store.StoreAsync(token.Revoke(now, "new-hash"));

        var result = await validator.ValidateAsync(
            new RefreshTokenValidationContext
            {
                Tenant = TenantKey.Single,
                RefreshToken = raw,
                Now = now,
                Device = DeviceContext.Create(DeviceId.Create(ValidDeviceId), null, null, null, null, null),
            });

        Assert.False(result.IsValid);
        Assert.True(result.IsReuseDetected);
    }
}
