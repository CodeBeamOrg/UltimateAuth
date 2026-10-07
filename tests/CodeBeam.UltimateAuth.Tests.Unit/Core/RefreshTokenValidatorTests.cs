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

    private static UAuthRefreshTokenValidator CreateValidator(InMemoryRefreshTokenStoreFactory factory, ITokenHasher hasher)
    {
        return new UAuthRefreshTokenValidator(factory, hasher);
    }

    private static ITokenHasher CreateHasher()
    {
        return new HmacSha256TokenHasher(Encoding.UTF8.GetBytes("unit-test-secret-key"));
    }

    [Fact]
    public async Task Invalid_When_Token_Not_Found()
    {
        var factory = new InMemoryRefreshTokenStoreFactory();
        var hasher = CreateHasher();
        var validator = CreateValidator(factory, hasher);

        var result = await validator.ValidateAsync(
            new RefreshTokenValidationContext
            {
                Tenant = TenantKey.Single,
                RefreshToken = "non-existing",
                Now = DateTimeOffset.UtcNow,
                Device = DeviceContext.Create(DeviceId.Create(ValidDeviceId), null, null, null, null, null),
            });

        Assert.False(result.IsValid);
        Assert.Equal(RefreshTokenValidationState.NotFound, result.State);
    }

    [Fact]
    public async Task Invalid_When_Token_Is_Revoked_Without_Replacement()
    {
        var factory = new InMemoryRefreshTokenStoreFactory();
        var store = factory.Create(TenantKey.Single);

        var hasher = CreateHasher();
        var validator = CreateValidator(factory, hasher);

        var now = DateTimeOffset.UtcNow;

        const string rawToken = "refresh-token-1";
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

        await store.StoreAsync(
            token.Revoke(now));

        var result = await validator.ValidateAsync(
            new RefreshTokenValidationContext
            {
                Tenant = TenantKey.Single,
                RefreshToken = rawToken,
                Now = now,
                Device = DeviceContext.Create(
                    DeviceId.Create(ValidDeviceId),
                    null,
                    null,
                    null,
                    null,
                    null)
            });

        Assert.False(result.IsValid);
        Assert.Equal(
            RefreshTokenValidationState.Invalid,
            result.State);
    }

    [Fact]
    public async Task Consumed_When_Token_Was_Replaced_By_Rotation()
    {
        var factory = new InMemoryRefreshTokenStoreFactory();
        var store = factory.Create(TenantKey.Single);

        var hasher = CreateHasher();
        var validator = CreateValidator(factory, hasher);

        var now = DateTimeOffset.UtcNow;

        const string rawToken = "refresh-token-1";
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

        await store.StoreAsync(
            token.Revoke(
                now,
                "replacement-refresh-token-hash"));

        var result = await validator.ValidateAsync(
            new RefreshTokenValidationContext
            {
                Tenant = TenantKey.Single,
                RefreshToken = rawToken,
                Now = now,
                Device = DeviceContext.Create(
                    DeviceId.Create(ValidDeviceId),
                    null,
                    null,
                    null,
                    null,
                    null)
            });

        Assert.False(result.IsValid);
        Assert.Equal(
            RefreshTokenValidationState.Consumed,
            result.State);

        Assert.Equal(
            "replacement-refresh-token-hash",
            result.ReplacedByTokenHash);
    }

    [Fact]
    public async Task Invalid_When_Expected_Session_Id_Does_Not_Match()
    {
        var factory =
            new InMemoryRefreshTokenStoreFactory();

        var store =
            factory.Create(TenantKey.Single);

        var hasher =
            CreateHasher();

        var validator =
            CreateValidator(factory, hasher);

        var now =
            DateTimeOffset.UtcNow;

        const string rawToken =
            "refresh-token-2";

        var tokenHash =
            hasher.Hash(rawToken);

        var token = RefreshToken.Create(
            TokenId.New(),
            tokenHash,
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
                RefreshToken = rawToken,
                ExpectedSessionId =
                    TestIds.Session(
                        "session-2-cccccccccccccccccccccc"),
                Now = now,
                Device = DeviceContext.Create(
                    DeviceId.Create(ValidDeviceId),
                    null,
                    null,
                    null,
                    null,
                    null),
            });

        Assert.False(result.IsValid);

        Assert.Equal(
            RefreshTokenValidationState.Invalid,
            result.State);
    }

    [Fact]
    public async Task Invalid_When_Token_Is_Expired()
    {
        var factory =
            new InMemoryRefreshTokenStoreFactory();

        var store =
            factory.Create(TenantKey.Single);

        var hasher = CreateHasher();
        var validator = CreateValidator(factory, hasher);

        var now =
            DateTimeOffset.UtcNow;

        const string rawToken =
            "expired-refresh-token";

        var tokenHash =
            hasher.Hash(rawToken);

        var token = RefreshToken.Create(
            TokenId.New(),
            tokenHash,
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
                RefreshToken = rawToken,
                Now = now,
                Device = DeviceContext.Create(
                    DeviceId.Create(ValidDeviceId),
                    null,
                    null,
                    null,
                    null,
                    null),
            });

        Assert.False(result.IsValid);

        Assert.Equal(
            RefreshTokenValidationState.Expired,
            result.State);
    }

    [Fact]
    public async Task Valid_When_Token_Is_Active()
    {
        var factory = new InMemoryRefreshTokenStoreFactory();
        var store = factory.Create(TenantKey.Single);

        var hasher = CreateHasher();
        var validator = CreateValidator(factory, hasher);

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
        Assert.Equal(RefreshTokenValidationState.Valid, result.State);
    }

    [Fact]
    public async Task Reuse_Detected_When_Old_Token_Is_Reused_After_Rotation()
    {
        var factory = new InMemoryRefreshTokenStoreFactory();
        var store = factory.Create(TenantKey.Single);

        var hasher = CreateHasher();
        var validator = CreateValidator(factory, hasher);

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
        Assert.Equal(RefreshTokenValidationState.Consumed, result.State);
    }
}
