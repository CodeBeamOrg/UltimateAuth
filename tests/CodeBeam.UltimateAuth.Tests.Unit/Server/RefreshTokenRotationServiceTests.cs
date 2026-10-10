using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Errors;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Abstactions;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Server.Services;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;
using System.Text.Json;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server;

public sealed class RefreshTokenRotationServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 20, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task RotateAsync_WhenValidationIsInvalid_ReturnsFailedWithoutCreatingStoreOrIssuingTokens()
    {
        var validator = new Mock<IRefreshTokenValidator>();
        var storeFactory = new Mock<IRefreshTokenStoreFactory>();
        var issuer = new Mock<ITokenIssuer>();
        var claimsProvider = new Mock<IUserClaimsProvider>();
        var clock = new TestClock(Now);

        validator
            .Setup(x => x.ValidateAsync(It.IsAny<RefreshTokenValidationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RefreshTokenValidationResult.Invalid());

        var sut = new RefreshTokenRotationService(validator.Object, storeFactory.Object, issuer.Object, claimsProvider.Object);

        var result = await sut.RotateAsync(CreateFlow(), CreateContext());

        result.Result.IsSuccess.Should().BeFalse();
        result.Result.ReauthRequired.Should().BeTrue();

        storeFactory.Verify(x => x.Create(It.IsAny<TenantKey>()), Times.Never);
        issuer.Verify(x => x.IssueAccessTokenAsync(
            It.IsAny<AuthFlowContext>(),
            It.IsAny<TokenIssuanceContext>(),
            It.IsAny<CancellationToken>()), Times.Never);
        issuer.Verify(x => x.IssueRefreshTokenAsync(
            It.IsAny<AuthFlowContext>(),
            It.IsAny<TokenIssuanceContext>(),
            It.IsAny<RefreshTokenPersistence>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RotateAsync_WhenTokenIsNotFound_ReturnsFailedWithoutCreatingStoreOrIssuingTokens()
    {
        var validator = CreateValidator(
            RefreshTokenValidationResult.NotFound());

        var storeFactory =
            new Mock<IRefreshTokenStoreFactory>();

        var issuer =
            new Mock<ITokenIssuer>();
        var claimsProvider = new Mock<IUserClaimsProvider>();

        var sut = new RefreshTokenRotationService(
            validator.Object,
            storeFactory.Object,
            issuer.Object,
            claimsProvider.Object);

        var result = await sut.RotateAsync(
            CreateFlow(),
            CreateContext());

        result.Result.IsSuccess.Should().BeFalse();
        result.Result.ReauthRequired.Should().BeTrue();

        storeFactory.Verify(
            x => x.Create(It.IsAny<TenantKey>()),
            Times.Never);

        issuer.Verify(
            x => x.IssueRefreshTokenAsync(
                It.IsAny<AuthFlowContext>(),
                It.IsAny<TokenIssuanceContext>(),
                It.IsAny<RefreshTokenPersistence>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        issuer.Verify(
            x => x.IssueAccessTokenAsync(
                It.IsAny<AuthFlowContext>(),
                It.IsAny<TokenIssuanceContext>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RotateAsync_WhenTokenIsExpired_ReturnsFailedWithoutMutatingStore()
    {
        var tenant = TenantKey.FromExternal("tenant-a");

        var token = RefreshToken.Create(
            tokenId: TokenId.New(),
            tokenHash: "expired-refresh-token-hash",
            tenant: tenant,
            userKey: UserKey.New(),
            sessionId: TestIds.Session("expired-session"),
            chainId: SessionChainId.New(),
            createdAt: Now.AddDays(-8),
            expiresAt: Now.AddMinutes(-1));

        var validator = CreateValidator(
            RefreshTokenValidationResult.Expired(token));

        var storeFactory =
            new Mock<IRefreshTokenStoreFactory>();

        var issuer =
            new Mock<ITokenIssuer>();
        var claimsProvider = new Mock<IUserClaimsProvider>();

        var sut = new RefreshTokenRotationService(
            validator.Object,
            storeFactory.Object,
            issuer.Object,
            claimsProvider.Object);

        var result = await sut.RotateAsync(
            CreateFlow(tenant, multiTenant: true),
            CreateContext(token.SessionId));

        result.Result.IsSuccess.Should().BeFalse();

        storeFactory.Verify(
            x => x.Create(It.IsAny<TenantKey>()),
            Times.Never);

        issuer.Verify(
            x => x.IssueRefreshTokenAsync(
                It.IsAny<AuthFlowContext>(),
                It.IsAny<TokenIssuanceContext>(),
                It.IsAny<RefreshTokenPersistence>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RotateAsync_WhenTokenIsAlreadyConsumed_ShouldRevokeCompromisedChainWithoutIssuingTokens()
    {
        var tenant =
            TenantKey.FromExternal("tenant-a");

        var chainId =
            SessionChainId.New();

        var sessionId =
            TestIds.Session("session-consumed");

        var token = RefreshToken.Create(
            tokenId: TokenId.New(),
            tokenHash: "old-refresh-token-hash",
            tenant: tenant,
            userKey: UserKey.New(),
            sessionId: sessionId,
            chainId: chainId,
            createdAt: Now.AddDays(-1),
            expiresAt: Now.AddDays(6))
            .Revoke(
                Now.AddMinutes(-1),
                "replacement-refresh-token-hash");

        var validator =
            new Mock<IRefreshTokenValidator>();

        var store =
            new Mock<IRefreshTokenStore>();

        var storeFactory =
            new Mock<IRefreshTokenStoreFactory>();

        var issuer =
            new Mock<ITokenIssuer>();
        var claimsProvider = new Mock<IUserClaimsProvider>();

        validator
            .Setup(x => x.ValidateAsync(
                It.IsAny<RefreshTokenValidationContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(
                RefreshTokenValidationResult.Consumed(token));

        storeFactory
            .Setup(x => x.Create(tenant))
            .Returns(store.Object);

        store
            .Setup(x => x.ExecuteAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns(
                (Func<CancellationToken, Task> action,
                 CancellationToken ct) =>
                    action(ct));

        store
            .Setup(x => x.RevokeByChainAsync(
                chainId,
                It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut =
            new RefreshTokenRotationService(
                validator.Object,
                storeFactory.Object,
                issuer.Object,
                claimsProvider.Object);

        var result =
            await sut.RotateAsync(
                CreateFlow(
                    tenant,
                    multiTenant: true),
                CreateContext(sessionId));

        result.Result.IsSuccess.Should()
            .BeFalse();

        result.Result.ReauthRequired.Should()
            .BeTrue();

        storeFactory.Verify(
            x => x.Create(tenant),
            Times.Once);

        store.Verify(
            x => x.RevokeByChainAsync(
                chainId,
                Now,
                It.IsAny<CancellationToken>()),
            Times.Once);

        issuer.Verify(
            x => x.IssueRefreshTokenAsync(
                It.IsAny<AuthFlowContext>(),
                It.IsAny<TokenIssuanceContext>(),
                It.IsAny<RefreshTokenPersistence>(),
                It.IsAny<CancellationToken>()),
            Times.Never);

        issuer.Verify(
            x => x.IssueAccessTokenAsync(
                It.IsAny<AuthFlowContext>(),
                It.IsAny<TokenIssuanceContext>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RotateAsync_WhenRefreshTokenCannotBeIssued_ReturnsFailedWithoutConsumingOldToken()
    {
        var tenant = TenantKey.FromExternal("tenant-a");
        var validation = CreateValidValidation(tenant);

        var validator = CreateValidator(validation);
        var store = CreateExecutableStore();
        var storeFactory = CreateStoreFactory(tenant, store);
        var issuer = new Mock<ITokenIssuer>();
        var claimsProvider = new Mock<IUserClaimsProvider>();

        issuer
            .Setup(x => x.IssueRefreshTokenAsync(
                It.IsAny<AuthFlowContext>(),
                It.IsAny<TokenIssuanceContext>(),
                RefreshTokenPersistence.DoNotPersist,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshTokenIssuanceResult?)null);

        var sut = new RefreshTokenRotationService(
            validator.Object,
            storeFactory.Object,
            issuer.Object,
            claimsProvider.Object);

        var result = await sut.RotateAsync(
            CreateFlow(tenant, multiTenant: true),
            CreateContext(validation.SessionId));

        result.Result.IsSuccess.Should().BeFalse();
        result.Result.ReauthRequired.Should().BeTrue();

        store.Verify(x => x.TryConsumeAsync(
            It.IsAny<string>(),
            It.IsAny<DateTimeOffset>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()),
            Times.Never);

        store.Verify(x => x.StoreAsync(
            It.IsAny<RefreshToken>(),
            It.IsAny<CancellationToken>()),
            Times.Never);

        issuer.Verify(x => x.IssueAccessTokenAsync(
            It.IsAny<AuthFlowContext>(),
            It.IsAny<TokenIssuanceContext>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task RotateAsync_WhenValid_AtomicallyRotatesRefreshToken()
    {
        var tenant = TenantKey.FromExternal("tenant-a");
        var validation = CreateValidValidation(tenant);

        var validator = CreateValidator(validation);
        var store = CreateSuccessfulRotationStore();
        var storeFactory = CreateStoreFactory(tenant, store);
        var claimsProvider = new Mock<IUserClaimsProvider>();

        var issuer = new Mock<ITokenIssuer>();

        var accessToken = CreateAccessToken();
        var refreshToken = CreateRefreshTokenIssuanceResult();

        issuer
            .Setup(x => x.IssueAccessTokenAsync(
                It.IsAny<AuthFlowContext>(),
                It.IsAny<TokenIssuanceContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(accessToken);

        issuer
            .Setup(x => x.IssueRefreshTokenAsync(
                It.IsAny<AuthFlowContext>(),
                It.IsAny<TokenIssuanceContext>(),
                RefreshTokenPersistence.DoNotPersist,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshToken);

        var sut = new RefreshTokenRotationService(validator.Object, storeFactory.Object, issuer.Object, claimsProvider.Object);

        var result = await sut.RotateAsync(CreateFlow(tenant, multiTenant: true), CreateContext(validation.SessionId));

        result.Result.IsSuccess.Should().BeTrue();
        result.Result.AccessToken.Should().BeSameAs(accessToken);
        result.Result.RefreshToken.Should().NotBeNull();
        result.Result.RefreshToken!.Token.Should().Be(refreshToken.Token);
        result.Result.RefreshToken.ExpiresAt.Should().Be(refreshToken.ExpiresAt);
        typeof(RefreshTokenInfo).GetProperty(nameof(RefreshTokenIssuanceResult.TokenHash)).Should().BeNull();

        result.Tenant.Should().Be(tenant);
        result.UserKey.Should().Be(validation.UserKey);
        result.SessionId.Should().Be(validation.SessionId);
        result.ChainId.Should().Be(validation.ChainId);

        store.Verify(x => x.TryConsumeAsync(
            validation.TokenHash!,
            Now,
            refreshToken.TokenHash,
            It.IsAny<CancellationToken>()),
            Times.Once);

        store.Verify(x => x.StoreAsync(
            It.Is<RefreshToken>(token =>
                token.TokenHash == refreshToken.TokenHash &&
                token.Tenant == tenant &&
                token.UserKey == validation.UserKey &&
                token.SessionId == validation.SessionId &&
                token.ChainId == validation.ChainId &&
                token.CreatedAt == Now &&
                token.ExpiresAt == refreshToken.ExpiresAt),
            It.IsAny<CancellationToken>()),
            Times.Once);

        issuer.Verify(x => x.IssueAccessTokenAsync(
            It.IsAny<AuthFlowContext>(),
            It.Is<TokenIssuanceContext>(ctx =>
                ctx.UserKey == validation.UserKey &&
                ctx.SessionId == validation.SessionId &&
                ctx.ChainId == validation.ChainId),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task RotateAsync_WhenMultiTenantEnabled_UsesValidatedTenantForTokenIssuance()
    {
        var tenant = TenantKey.FromExternal("tenant-a");

        var validation =
            CreateValidValidation(tenant);

        var issuer =
            new Mock<ITokenIssuer>();
        var claimsProvider = new Mock<IUserClaimsProvider>();

        TokenIssuanceContext? captured = null;

        SetupSuccessfulIssuer(
            issuer,
            ctx => captured = ctx);

        var store =
            CreateSuccessfulRotationStore();

        var sut =
            new RefreshTokenRotationService(
                CreateValidator(validation).Object,
                CreateStoreFactory(tenant, store).Object,
                issuer.Object,
                claimsProvider.Object);

        var result = await sut.RotateAsync(
            CreateFlow(
                tenant,
                multiTenant: true),
            CreateContext(validation.SessionId));

        result.Result.IsSuccess.Should().BeTrue();

        captured.Should().NotBeNull();

        captured!.Tenant.Should()
            .Be(tenant);
    }

    [Fact]
    public async Task RotateAsync_WhenMultiTenantDisabled_UsesSingleTenantForTokenIssuance()
    {
        var validation =
            CreateValidValidation(TenantKey.Single);

        var issuer =
            new Mock<ITokenIssuer>();
        var claimsProvider = new Mock<IUserClaimsProvider>();

        TokenIssuanceContext? captured = null;

        SetupSuccessfulIssuer(
            issuer,
            ctx => captured = ctx);

        var store =
            CreateSuccessfulRotationStore();

        var sut =
            new RefreshTokenRotationService(
                CreateValidator(validation).Object,
                CreateStoreFactory(
                    TenantKey.Single,
                    store).Object,
                issuer.Object,
                claimsProvider.Object);

        var result = await sut.RotateAsync(
            CreateFlow(
                TenantKey.Single,
                multiTenant: false),
            CreateContext(validation.SessionId));

        result.Result.IsSuccess.Should().BeTrue();

        captured.Should().NotBeNull();

        captured!.Tenant.Should()
            .Be(TenantKey.Single);
    }

    [Fact]
    public async Task RotateAsync_WhenAtomicConsumeLosesRace_ReturnsFailedWithoutStoringReplacement()
    {
        var tenant = TenantKey.FromExternal("tenant-a");
        var validation = CreateValidValidation(tenant);

        var validator = CreateValidator(validation);
        var store = CreateExecutableStore();
        var storeFactory = CreateStoreFactory(tenant, store);
        var claimsProvider = new Mock<IUserClaimsProvider>();

        var issuer = new Mock<ITokenIssuer>();

        var accessToken = CreateAccessToken();
        var refreshToken = CreateRefreshTokenIssuanceResult();

        issuer
            .Setup(x => x.IssueAccessTokenAsync(
                It.IsAny<AuthFlowContext>(),
                It.IsAny<TokenIssuanceContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(accessToken);

        issuer
            .Setup(x => x.IssueRefreshTokenAsync(
                It.IsAny<AuthFlowContext>(),
                It.IsAny<TokenIssuanceContext>(),
                RefreshTokenPersistence.DoNotPersist,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(refreshToken);

        store
            .Setup(x => x.TryConsumeAsync(
                validation.TokenHash!,
                Now,
                refreshToken.TokenHash,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var sut = new RefreshTokenRotationService(
            validator.Object,
            storeFactory.Object,
            issuer.Object,
            claimsProvider.Object);

        var result = await sut.RotateAsync(
            CreateFlow(tenant, multiTenant: true),
            CreateContext(validation.SessionId));

        result.Result.IsSuccess.Should().BeFalse();
        result.Result.ReauthRequired.Should().BeTrue();

        store.Verify(x => x.TryConsumeAsync(
            validation.TokenHash!,
            Now,
            refreshToken.TokenHash,
            It.IsAny<CancellationToken>()),
            Times.Once);

        store.Verify(x => x.StoreAsync(
            It.IsAny<RefreshToken>(),
            It.IsAny<CancellationToken>()),
            Times.Never);

        issuer.Verify(x => x.IssueAccessTokenAsync(
            It.IsAny<AuthFlowContext>(),
            It.IsAny<TokenIssuanceContext>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public void RefreshTokenInfo_DoesNotExposeTokenHash()
    {
        var info = new RefreshTokenInfo
        {
            Token = "raw-refresh-token",
            ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
        };

        var json = JsonSerializer.Serialize(info);

        json.Should().Contain("raw-refresh-token");
        json.Should().NotContain("TokenHash");
        typeof(RefreshTokenInfo).GetProperty("TokenHash").Should().BeNull();
    }


    private static Mock<IRefreshTokenValidator> CreateValidator(RefreshTokenValidationResult result)
    {
        var validator = new Mock<IRefreshTokenValidator>();
        validator
            .Setup(x => x.ValidateAsync(It.IsAny<RefreshTokenValidationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(result);
        return validator;
    }

    private static Mock<IRefreshTokenStoreFactory> CreateStoreFactory(
        TenantKey tenant,
        Mock<IRefreshTokenStore> store)
    {
        var factory = new Mock<IRefreshTokenStoreFactory>();
        factory.Setup(x => x.Create(tenant)).Returns(store.Object);
        return factory;
    }

    private static Mock<IRefreshTokenStore> CreateExecutableStore()
    {
        var store = new Mock<IRefreshTokenStore>();

        store
            .Setup(x => x.ExecuteAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>(
                (action, ct) => action(ct));

        store
            .Setup(x => x.ExecuteAsync(
                It.IsAny<Func<CancellationToken, Task<bool>>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task<bool>>, CancellationToken>(
                (action, ct) => action(ct));

        return store;
    }

    private static Mock<IRefreshTokenStore> CreateSuccessfulRotationStore()
    {
        var store = CreateExecutableStore();

        store
            .Setup(x => x.TryConsumeAsync(
                It.IsAny<string>(),
                It.IsAny<DateTimeOffset>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        return store;
    }

    private static RefreshTokenValidationResult CreateValidValidation(TenantKey tenant)
    {
        var token = RefreshToken.Create(
            tokenId: TokenId.New(),
            tokenHash: "old-refresh-token-hash",
            tenant: tenant,
            userKey: UserKey.New(),
            sessionId: TestIds.Session("rotation-session"),
            chainId: SessionChainId.New(),
            createdAt: Now.AddDays(-1),
            expiresAt: Now.AddDays(7));

        return RefreshTokenValidationResult.Valid(token, token.TokenHash);
    }

    private static RefreshTokenRotationContext CreateContext(AuthSessionId? expectedSessionId = null)
        => new()
        {
            RefreshToken = "old-refresh-token",
            Now = Now,
            Device = TestDevice.Default(),
            ExpectedSessionId = expectedSessionId
        };

    private static AccessToken CreateAccessToken()
        => new()
        {
            Token = "access-token",
            Format = TokenFormat.Jwt,
            ExpiresAt = Now.AddMinutes(15)
        };

    private static RefreshTokenIssuanceResult CreateRefreshTokenIssuanceResult()
        => new()
        {
            Token = "new-refresh-token",
            TokenHash = "new-refresh-token-hash",
            ExpiresAt = Now.AddDays(7)
        };

    private static void SetupSuccessfulIssuer(
        Mock<ITokenIssuer> issuer,
        Action<TokenIssuanceContext>? capture = null)
    {
        issuer
            .Setup(x => x.IssueAccessTokenAsync(
                It.IsAny<AuthFlowContext>(),
                It.IsAny<TokenIssuanceContext>(),
                It.IsAny<CancellationToken>()))
            .Callback<AuthFlowContext, TokenIssuanceContext, CancellationToken>((_, ctx, _) => capture?.Invoke(ctx))
            .ReturnsAsync(CreateAccessToken());

        issuer
            .Setup(x => x.IssueRefreshTokenAsync(
                It.IsAny<AuthFlowContext>(),
                It.IsAny<TokenIssuanceContext>(),
                RefreshTokenPersistence.DoNotPersist,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateRefreshTokenIssuanceResult());
    }

    private static AuthFlowContext CreateFlow(TenantKey? tenant = null, bool multiTenant = false)
    {
        var resolvedTenant = tenant ?? TenantKey.Single;
        var options = TestServerOptions.Default();
        options.MultiTenant.Enabled = multiTenant;

        return new AuthFlowContext(
            flowType: AuthFlowType.RefreshSession,
            clientProfile: UAuthClientProfile.Api,
            effectiveMode: UAuthMode.Hybrid,
            device: TestDevice.Default(),
            tenantKey: resolvedTenant,
            isAuthenticated: true,
            userKey: UserKey.New(),
            session: null,
            originalOptions: options,
            effectiveOptions: TestServerOptions.Effective(UAuthMode.Hybrid),
            response: new EffectiveAuthResponse(
                sessionIdDelivery: CredentialResponseOptions.Disabled(GrantKind.Session),
                accessTokenDelivery: CredentialResponseOptions.Disabled(GrantKind.AccessToken),
                refreshTokenDelivery: CredentialResponseOptions.Disabled(GrantKind.RefreshToken),
                redirect: EffectiveRedirectResponse.Disabled),
            primaryTokenKind: PrimaryTokenKind.AccessToken,
            returnUrlInfo: ReturnUrlInfo.None());
    }
}
