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
        var clock = new TestClock(Now);

        validator
            .Setup(x => x.ValidateAsync(It.IsAny<RefreshTokenValidationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RefreshTokenValidationResult.Invalid());

        var sut = new RefreshTokenRotationService(validator.Object, storeFactory.Object, issuer.Object);

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
    public async Task RotateAsync_WhenReuseIsDetectedForChain_RevokesChainAndReturnsFailed()
    {
        var tenant = TenantKey.FromExternal("tenant-a");
        var sessionId = TestIds.Session("session-reuse-chain");
        var chainId = SessionChainId.New();

        var validator = new Mock<IRefreshTokenValidator>();
        var storeFactory = new Mock<IRefreshTokenStoreFactory>();
        var store = new Mock<IRefreshTokenStore>();
        var issuer = new Mock<ITokenIssuer>();

        validator
            .Setup(x => x.ValidateAsync(It.IsAny<RefreshTokenValidationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RefreshTokenValidationResult.ReuseDetected(
                tenant,
                sessionId: sessionId,
                tokenHash: "old-hash",
                chainId: chainId,
                userKey: UserKey.New()));

        storeFactory.Setup(x => x.Create(tenant)).Returns(store.Object);

        var sut = new RefreshTokenRotationService(
            validator.Object,
            storeFactory.Object,
            issuer.Object);

        var result = await sut.RotateAsync(CreateFlow(tenant, multiTenant: true), CreateContext(sessionId));

        result.Result.IsSuccess.Should().BeFalse();
        result.Result.ReauthRequired.Should().BeTrue();

        store.Verify(x => x.RevokeByChainAsync(chainId, Now, It.IsAny<CancellationToken>()), Times.Once);
        store.Verify(x => x.RevokeBySessionAsync(
            It.IsAny<AuthSessionId>(),
            It.IsAny<DateTimeOffset>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RotateAsync_WhenReuseIsDetectedWithoutChain_RevokesSessionAndReturnsFailed()
    {
        var tenant = TenantKey.FromExternal("tenant-a");
        var sessionId = TestIds.Session("session-reuse-session");

        var validator = new Mock<IRefreshTokenValidator>();
        var storeFactory = new Mock<IRefreshTokenStoreFactory>();
        var store = new Mock<IRefreshTokenStore>();

        validator
            .Setup(x => x.ValidateAsync(It.IsAny<RefreshTokenValidationContext>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(RefreshTokenValidationResult.ReuseDetected(
                tenant,
                sessionId: sessionId,
                tokenHash: "old-hash",
                userKey: UserKey.New()));

        storeFactory.Setup(x => x.Create(tenant)).Returns(store.Object);

        var sut = new RefreshTokenRotationService(
            validator.Object,
            storeFactory.Object,
            Mock.Of<ITokenIssuer>());

        var result = await sut.RotateAsync(CreateFlow(tenant, multiTenant: true), CreateContext(sessionId));

        result.Result.IsSuccess.Should().BeFalse();

        store.Verify(x => x.RevokeBySessionAsync(sessionId, Now, It.IsAny<CancellationToken>()), Times.Once);
        store.Verify(x => x.RevokeByChainAsync(
            It.IsAny<SessionChainId>(),
            It.IsAny<DateTimeOffset>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RotateAsync_WhenValidatedTokenHashIsMissing_ThrowsValidationException()
    {
        var tenant = TenantKey.FromExternal("tenant-a");
        var validation = RefreshTokenValidationResult.Valid(
            tenant,
            UserKey.New(),
            TestIds.Session("session-missing-hash"),
            tokenHash: null,
            chainId: SessionChainId.New());

        var validator = new Mock<IRefreshTokenValidator>();
        var storeFactory = new Mock<IRefreshTokenStoreFactory>();

        validator
            .Setup(x => x.ValidateAsync(
                It.IsAny<RefreshTokenValidationContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(validation);

        storeFactory
            .Setup(x => x.Create(tenant))
            .Returns(Mock.Of<IRefreshTokenStore>());

        var sut = new RefreshTokenRotationService(
            validator.Object,
            storeFactory.Object,
            Mock.Of<ITokenIssuer>());

        var act = () => sut.RotateAsync(
            CreateFlow(tenant, multiTenant: true),
            CreateContext(validation.SessionId));

        await act.Should().ThrowAsync<UAuthValidationException>();
    }

    [Fact]
    public async Task RotateAsync_WhenRefreshTokenCannotBeIssued_ReturnsFailedWithoutRevokingOldToken()
    {
        var tenant = TenantKey.FromExternal("tenant-a");
        var validation = CreateValidValidation(tenant);
        var validator = CreateValidator(validation);
        var store = new Mock<IRefreshTokenStore>();
        var storeFactory = CreateStoreFactory(tenant, store);
        var issuer = new Mock<ITokenIssuer>();

        issuer
            .Setup(x => x.IssueAccessTokenAsync(
                It.IsAny<AuthFlowContext>(),
                It.IsAny<TokenIssuanceContext>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateAccessToken());

        issuer
            .Setup(x => x.IssueRefreshTokenAsync(
                It.IsAny<AuthFlowContext>(),
                It.IsAny<TokenIssuanceContext>(),
                RefreshTokenPersistence.DoNotPersist,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshTokenInfo?)null);

        var sut = new RefreshTokenRotationService(validator.Object, storeFactory.Object, issuer.Object);

        var result = await sut.RotateAsync(CreateFlow(tenant, multiTenant: true), CreateContext(validation.SessionId));

        result.Result.IsSuccess.Should().BeFalse();

        store.Verify(x => x.ExecuteAsync(
            It.IsAny<Func<CancellationToken, Task>>(),
            It.IsAny<CancellationToken>()), Times.Never);
        store.Verify(x => x.RevokeAsync(
            It.IsAny<string>(),
            It.IsAny<DateTimeOffset>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
        store.Verify(x => x.StoreAsync(
            It.IsAny<RefreshToken>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RotateAsync_WhenValid_RotatesAndStoresReplacementInSingleStoreExecution()
    {
        var tenant = TenantKey.FromExternal("tenant-a");
        var validation = CreateValidValidation(tenant);
        var validator = CreateValidator(validation);
        var store = new Mock<IRefreshTokenStore>();
        var storeFactory = CreateStoreFactory(tenant, store);
        var issuer = new Mock<ITokenIssuer>();
        var accessToken = CreateAccessToken();
        var refreshToken = CreateRefreshTokenInfo();

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
            .Setup(x => x.ExecuteAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>((action, ct) => action(ct));

        var sut = new RefreshTokenRotationService(validator.Object, storeFactory.Object, issuer.Object);

        var result = await sut.RotateAsync(CreateFlow(tenant, multiTenant: true), CreateContext(validation.SessionId));

        result.Result.IsSuccess.Should().BeTrue();
        result.Result.AccessToken.Should().BeSameAs(accessToken);
        result.Result.RefreshToken.Should().BeSameAs(refreshToken);
        result.Tenant.Should().Be(tenant);
        result.UserKey.Should().Be(validation.UserKey);
        result.SessionId.Should().Be(validation.SessionId);
        result.ChainId.Should().Be(validation.ChainId);

        store.Verify(x => x.RevokeAsync(
            validation.TokenHash!,
            Now,
            refreshToken.TokenHash,
            It.IsAny<CancellationToken>()), Times.Once);

        store.Verify(x => x.StoreAsync(
            It.Is<RefreshToken>(token =>
                token.TokenHash == refreshToken.TokenHash &&
                token.Tenant == tenant &&
                token.UserKey == validation.UserKey!.Value &&
                token.SessionId == validation.SessionId!.Value &&
                token.ChainId == validation.ChainId &&
                token.CreatedAt == Now &&
                token.ExpiresAt == refreshToken.ExpiresAt),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RotateAsync_WhenMultiTenantEnabled_UsesValidatedTenantForTokenIssuance()
    {
        var tenant = TenantKey.FromExternal("tenant-a");
        var validation = CreateValidValidation(tenant);
        var issuer = new Mock<ITokenIssuer>();
        TokenIssuanceContext? captured = null;

        SetupSuccessfulIssuer(issuer, ctx => captured = ctx);

        var store = CreateExecutableStore();
        var sut = new RefreshTokenRotationService(
            CreateValidator(validation).Object,
            CreateStoreFactory(tenant, store).Object,
            issuer.Object);

        await sut.RotateAsync(CreateFlow(tenant, multiTenant: true), CreateContext(validation.SessionId));

        captured.Should().NotBeNull();
        captured!.Tenant.Should().Be(tenant);
    }

    [Fact]
    public async Task RotateAsync_WhenMultiTenantDisabled_UsesSingleTenantForTokenIssuance()
    {
        var validation = CreateValidValidation(TenantKey.Single);
        var issuer = new Mock<ITokenIssuer>();
        TokenIssuanceContext? captured = null;

        SetupSuccessfulIssuer(issuer, ctx => captured = ctx);

        var store = CreateExecutableStore();
        var sut = new RefreshTokenRotationService(
            CreateValidator(validation).Object,
            CreateStoreFactory(TenantKey.Single, store).Object,
            issuer.Object);

        await sut.RotateAsync(CreateFlow(TenantKey.Single, multiTenant: false), CreateContext(validation.SessionId));

        captured.Should().NotBeNull();
        captured!.Tenant.Should().Be(TenantKey.Single);
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
            .Returns<Func<CancellationToken, Task>, CancellationToken>((action, ct) => action(ct));
        return store;
    }

    private static RefreshTokenValidationResult CreateValidValidation(TenantKey tenant)
        => RefreshTokenValidationResult.Valid(
            tenant,
            UserKey.New(),
            TestIds.Session("rotation-session"),
            "old-refresh-token-hash",
            SessionChainId.New());

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

    private static RefreshTokenInfo CreateRefreshTokenInfo()
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
            .ReturnsAsync(CreateRefreshTokenInfo());
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
