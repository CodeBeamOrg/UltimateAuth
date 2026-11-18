using AwesomeAssertions;
using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Server.Abstractions;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Server.Services;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using CodeBeam.UltimateAuth.Users;
using CodeBeam.UltimateAuth.Users.Contracts;
using Moq;
using System.Text.Json;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server;

public sealed class RefreshTokenRotationServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 18, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset AuthenticatedAt = Now.AddHours(-3);

    [Theory]
    [InlineData(UAuthMode.Hybrid)]
    [InlineData(UAuthMode.SemiHybrid)]
    [InlineData(UAuthMode.PureJwt)]
    public async Task RotateAsync_ValidToken_RotatesAndPreservesLineage(UAuthMode mode)
    {
        var test = new Fixture(mode);
        var original = test.CreateToken();
        test.Validate(original);
        test.IssueSuccessfully();
        test.ConsumeSuccessfully();

        var result = await test.RotateAsync(original.SessionId);

        result.Result.IsSuccess.Should().BeTrue();
        result.Result.ReauthRequired.Should().BeFalse();
        result.Result.AccessToken.Should().BeSameAs(test.AccessToken);
        result.Result.RefreshToken.Should().NotBeNull();
        result.Result.RefreshToken!.Token.Should().Be(test.RefreshIssuance.Token);
        result.Result.RefreshToken.ExpiresAt.Should().Be(test.RefreshIssuance.ExpiresAt);
        result.Tenant.Should().Be(original.Tenant);
        result.UserKey.Should().Be(original.UserKey);
        result.SessionId.Should().Be(original.SessionId);
        result.SessionChainId.Should().Be(original.SessionChainId);

        test.Store.Verify(x => x.TryConsumeAsync(
            original.TokenHash, Now, test.RefreshIssuance.TokenHash,
            It.IsAny<CancellationToken>()), Times.Once);

        test.Store.Verify(x => x.StoreAsync(
            It.Is<RefreshToken>(replacement =>
                replacement.TokenHash == test.RefreshIssuance.TokenHash &&
                replacement.Tenant == original.Tenant &&
                replacement.UserKey == original.UserKey &&
                replacement.SessionId == original.SessionId &&
                replacement.SessionChainId == original.SessionChainId &&
                replacement.RefreshChainId == original.RefreshChainId &&
                replacement.DeviceId == original.DeviceId &&
                replacement.AuthenticatedAt == AuthenticatedAt &&
                replacement.CreatedAt == Now &&
                replacement.ExpiresAt == test.RefreshIssuance.ExpiresAt),
            It.IsAny<CancellationToken>()), Times.Once);

        test.Issuer.Verify(x => x.IssueRefreshTokenAsync(
            It.IsAny<AuthFlowContext>(),
            It.Is<TokenIssuanceContext>(c =>
                c.RefreshChainId == original.RefreshChainId &&
                c.SessionId == original.SessionId &&
                c.SessionChainId == original.SessionChainId &&
                c.DeviceId == original.DeviceId &&
                c.AuthenticatedAt == AuthenticatedAt),
            RefreshTokenPersistence.DoNotPersist,
            It.IsAny<CancellationToken>()), Times.Once);

        test.Issuer.Verify(x => x.IssueAccessTokenAsync(
            It.IsAny<AuthFlowContext>(),
            It.Is<TokenIssuanceContext>(c =>
                c.UserKey == original.UserKey &&
                c.RefreshChainId == original.RefreshChainId &&
                c.SessionId == original.SessionId &&
                c.SessionChainId == original.SessionChainId &&
                c.DeviceId == original.DeviceId &&
                c.AuthenticatedAt == AuthenticatedAt),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RotateAsync_PureJwt_UsesUpdatedProfileAndOriginalAuthenticatedAt()
    {
        var test = new Fixture(UAuthMode.PureJwt);
        var original = test.CreateToken();
        test.Validate(original);
        test.IssueSuccessfully();
        test.ConsumeSuccessfully();
        test.SetProfile("updated-admin", "Updated Administrator", "Europe/Istanbul");

        TokenIssuanceContext? refreshContext = null;
        TokenIssuanceContext? accessContext = null;
        test.Issuer.Setup(x => x.IssueRefreshTokenAsync(
                It.IsAny<AuthFlowContext>(), It.IsAny<TokenIssuanceContext>(),
                RefreshTokenPersistence.DoNotPersist, It.IsAny<CancellationToken>()))
            .Callback<AuthFlowContext, TokenIssuanceContext, RefreshTokenPersistence, CancellationToken>(
                (_, context, _, _) => refreshContext = context)
            .ReturnsAsync(test.RefreshIssuance);
        test.Issuer.Setup(x => x.IssueAccessTokenAsync(
                It.IsAny<AuthFlowContext>(), It.IsAny<TokenIssuanceContext>(),
                It.IsAny<CancellationToken>()))
            .Callback<AuthFlowContext, TokenIssuanceContext, CancellationToken>(
                (_, context, _) => accessContext = context)
            .ReturnsAsync(test.AccessToken);

        var result = await test.RotateAsync();

        result.Result.IsSuccess.Should().BeTrue();
        accessContext.Should().NotBeNull();
        refreshContext.Should().NotBeNull();
        accessContext.Should().BeSameAs(refreshContext);
        accessContext!.PrimaryUserName.Should().Be("updated-admin");
        accessContext.DisplayName.Should().Be("Updated Administrator");
        accessContext.TimeZone.Should().Be("Europe/Istanbul");
        accessContext.AuthenticatedAt.Should().Be(AuthenticatedAt);
        accessContext.SessionId.Should().BeNull();
        accessContext.SessionChainId.Should().BeNull();
        accessContext.DeviceId.Should().Be(original.DeviceId);

        test.Identifiers.Verify(x => x.GetAsync(test.Tenant, test.User,
            It.IsAny<CancellationToken>()), Times.Once);
        test.Profiles.Verify(x => x.GetAsync(test.Tenant, test.User,
            ProfileKey.Default, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RotateAsync_PureJwt_SecondRotationPreservesOriginalAuthenticatedAt()
    {
        var test = new Fixture(UAuthMode.PureJwt);
        var original = test.CreateToken();
        test.Validate(original);
        test.IssueSuccessfully();
        test.ConsumeSuccessfully();

        var replacements = new List<RefreshToken>();
        test.Store.Setup(x => x.StoreAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .Callback<RefreshToken, CancellationToken>((token, _) => replacements.Add(token))
            .Returns(Task.CompletedTask);

        (await test.RotateAsync()).Result.IsSuccess.Should().BeTrue();
        replacements.Should().HaveCount(1);
        var firstReplacement = replacements[0];
        firstReplacement.AuthenticatedAt.Should().Be(AuthenticatedAt);

        test.Validate(firstReplacement);
        test.IssueSuccessfully("third-raw-token", "third-hash");
        (await test.RotateAsync()).Result.IsSuccess.Should().BeTrue();

        replacements.Should().HaveCount(2);
        var secondReplacement = replacements[1];
        secondReplacement.AuthenticatedAt.Should().Be(AuthenticatedAt);
        secondReplacement.RefreshChainId.Should().Be(original.RefreshChainId);
        secondReplacement.DeviceId.Should().Be(original.DeviceId);
        secondReplacement.SessionId.Should().BeNull();
        secondReplacement.SessionChainId.Should().BeNull();
        secondReplacement.TokenHash.Should().Be("third-hash");
    }

    [Theory]
    [InlineData(UAuthMode.Hybrid)]
    [InlineData(UAuthMode.PureJwt)]
    public async Task RotateAsync_InvalidOrNotFound_ReturnsFailedWithoutMutation(UAuthMode mode)
    {
        foreach (var validation in new[]
        {
            RefreshTokenValidationResult.Invalid(),
            RefreshTokenValidationResult.NotFound()
        })
        {
            var test = new Fixture(mode);
            test.SetValidation(validation);

            var result = await test.RotateAsync();

            AssertFailed(result);
            test.StoreFactory.Verify(x => x.Create(It.IsAny<TenantKey>()), Times.Never);
            test.Issuer.VerifyNoOtherCalls();
            test.Claims.VerifyNoOtherCalls();
            test.Identifiers.VerifyNoOtherCalls();
            test.Profiles.VerifyNoOtherCalls();
        }
    }

    [Theory]
    [InlineData(UAuthMode.Hybrid)]
    [InlineData(UAuthMode.PureJwt)]
    public async Task RotateAsync_ExpiredToken_ReturnsFailedWithoutMutation(UAuthMode mode)
    {
        var test = new Fixture(mode);
        var expired = test.CreateToken() with { ExpiresAt = Now.AddSeconds(-1) };
        test.SetValidation(RefreshTokenValidationResult.Expired(expired));

        AssertFailed(await test.RotateAsync(expired.SessionId));
        test.StoreFactory.Verify(x => x.Create(It.IsAny<TenantKey>()), Times.Never);
        test.Issuer.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(UAuthMode.Hybrid)]
    [InlineData(UAuthMode.SemiHybrid)]
    [InlineData(UAuthMode.PureJwt)]
    public async Task RotateAsync_ConsumedOutsideConcurrencyWindow_RevokesExpectedChain(UAuthMode mode)
    {
        var test = new Fixture(mode);
        var consumed = test.CreateToken().Revoke(Now.AddMinutes(-5), "replacement-hash");
        test.SetValidation(RefreshTokenValidationResult.Consumed(consumed));
        test.Flow.Options.Token.RefreshTokenConcurrentRequestWindow = TimeSpan.FromSeconds(30);

        AssertFailed(await test.RotateAsync(consumed.SessionId));

        if (mode == UAuthMode.PureJwt)
        {
            test.Store.Verify(x => x.RevokeByRefreshChainAsync(
                consumed.RefreshChainId, Now, It.IsAny<CancellationToken>()), Times.Once);
            test.Store.Verify(x => x.RevokeBySessionChainAsync(
                It.IsAny<SessionChainId>(), It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }
        else
        {
            test.Store.Verify(x => x.RevokeBySessionChainAsync(
                consumed.SessionChainId!.Value, Now, It.IsAny<CancellationToken>()), Times.Once);
            test.Store.Verify(x => x.RevokeByRefreshChainAsync(
                It.IsAny<RefreshChainId>(), It.IsAny<DateTimeOffset>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }

        test.Issuer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RotateAsync_SessionWithoutChain_ReplayRevokesSession()
    {
        var test = new Fixture(UAuthMode.Hybrid);
        var consumed = (test.CreateToken() with { SessionChainId = null })
            .Revoke(Now.AddMinutes(-5), "replacement-hash");
        test.SetValidation(RefreshTokenValidationResult.Consumed(consumed));
        test.Flow.Options.Token.RefreshTokenConcurrentRequestWindow = TimeSpan.FromSeconds(30);

        AssertFailed(await test.RotateAsync(consumed.SessionId));

        test.Store.Verify(x => x.RevokeBySessionAsync(
            consumed.SessionId!.Value, Now, It.IsAny<CancellationToken>()), Times.Once);
        test.Store.Verify(x => x.RevokeBySessionChainAsync(
            It.IsAny<SessionChainId>(), It.IsAny<DateTimeOffset>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(UAuthMode.Hybrid)]
    [InlineData(UAuthMode.PureJwt)]
    public async Task RotateAsync_ConsumedWithinConcurrencyWindow_FailsWithoutRevocation(UAuthMode mode)
    {
        var test = new Fixture(mode);
        var consumed = test.CreateToken().Revoke(Now.AddSeconds(-1), "replacement-hash");
        test.SetValidation(RefreshTokenValidationResult.Consumed(consumed));
        test.Flow.Options.Token.RefreshTokenConcurrentRequestWindow = TimeSpan.FromSeconds(30);

        AssertFailed(await test.RotateAsync(consumed.SessionId));

        test.StoreFactory.Verify(x => x.Create(It.IsAny<TenantKey>()), Times.Never);
        test.Issuer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RotateAsync_ConsumedWithoutReplacementHash_DoesNotUseConcurrencyGrace()
    {
        var test = new Fixture(UAuthMode.PureJwt);
        var consumed = test.CreateToken().Revoke(Now.AddSeconds(-1));
        test.SetValidation(RefreshTokenValidationResult.Consumed(consumed));
        test.Flow.Options.Token.RefreshTokenConcurrentRequestWindow = TimeSpan.FromSeconds(30);

        AssertFailed(await test.RotateAsync());

        test.Store.Verify(x => x.RevokeByRefreshChainAsync(
            consumed.RefreshChainId, Now, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(UAuthMode.Hybrid)]
    [InlineData(UAuthMode.SemiHybrid)]
    public async Task RotateAsync_SessionBasedModeRejectsSessionFreeToken(UAuthMode mode)
    {
        var test = new Fixture(mode);
        test.Validate(test.CreateToken(sessionFree: true));

        AssertFailed(await test.RotateAsync());
        test.StoreFactory.Verify(x => x.Create(It.IsAny<TenantKey>()), Times.Never);
        test.Issuer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RotateAsync_PureJwtRejectsSessionBoundToken()
    {
        var test = new Fixture(UAuthMode.PureJwt);
        test.Validate(test.CreateToken(sessionFree: false));

        AssertFailed(await test.RotateAsync());
        test.StoreFactory.Verify(x => x.Create(It.IsAny<TenantKey>()), Times.Never);
        test.Issuer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RotateAsync_PureJwtRejectsTokenWithoutDeviceId()
    {
        var test = new Fixture(UAuthMode.PureJwt);
        test.Validate(test.CreateToken() with { DeviceId = null });

        AssertFailed(await test.RotateAsync());
        test.StoreFactory.Verify(x => x.Create(It.IsAny<TenantKey>()), Times.Never);
        test.Issuer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RotateAsync_EmptyRefreshChainId_FailsBeforeIssuance()
    {
        var test = new Fixture(UAuthMode.PureJwt);
        test.Validate(test.CreateToken() with { RefreshChainId = default });

        AssertFailed(await test.RotateAsync());
        test.StoreFactory.Verify(x => x.Create(It.IsAny<TenantKey>()), Times.Never);
        test.Issuer.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(UAuthMode.Hybrid)]
    [InlineData(UAuthMode.PureJwt)]
    public async Task RotateAsync_RefreshIssuanceReturnsNull_DoesNotConsumeOldToken(UAuthMode mode)
    {
        var test = new Fixture(mode);
        var original = test.CreateToken();
        test.Validate(original);
        test.Issuer.Setup(x => x.IssueRefreshTokenAsync(
                It.IsAny<AuthFlowContext>(), It.IsAny<TokenIssuanceContext>(),
                RefreshTokenPersistence.DoNotPersist, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RefreshTokenIssuanceResult?)null);

        AssertFailed(await test.RotateAsync(original.SessionId));
        test.AssertNoMutation();
        test.Issuer.Verify(x => x.IssueAccessTokenAsync(
            It.IsAny<AuthFlowContext>(), It.IsAny<TokenIssuanceContext>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(UAuthMode.Hybrid)]
    [InlineData(UAuthMode.PureJwt)]
    public async Task RotateAsync_AccessIssuanceThrows_DoesNotConsumeOldToken(UAuthMode mode)
    {
        var test = new Fixture(mode);
        var original = test.CreateToken();
        test.Validate(original);
        test.IssueSuccessfully();
        test.Issuer.Setup(x => x.IssueAccessTokenAsync(
                It.IsAny<AuthFlowContext>(), It.IsAny<TokenIssuanceContext>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("AT issuance failed"));

        var act = () => test.RotateAsync(original.SessionId);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("AT issuance failed");
        test.AssertNoMutation();
    }

    [Theory]
    [InlineData(UAuthMode.Hybrid)]
    [InlineData(UAuthMode.PureJwt)]
    public async Task RotateAsync_ConsumeLosesRace_DoesNotStoreReplacement(UAuthMode mode)
    {
        var test = new Fixture(mode);
        var original = test.CreateToken();
        test.Validate(original);
        test.IssueSuccessfully();
        // Unconfigured TryConsumeAsync returns false.

        AssertFailed(await test.RotateAsync(original.SessionId));

        test.Store.Verify(x => x.TryConsumeAsync(
            original.TokenHash, Now, test.RefreshIssuance.TokenHash,
            It.IsAny<CancellationToken>()), Times.Once);
        test.Store.Verify(x => x.StoreAsync(
            It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
        test.Issuer.Verify(x => x.IssueAccessTokenAsync(
            It.IsAny<AuthFlowContext>(), It.IsAny<TokenIssuanceContext>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RotateAsync_StoreReplacementThrows_PropagatesFailure()
    {
        var test = new Fixture(UAuthMode.PureJwt);
        test.Validate(test.CreateToken());
        test.IssueSuccessfully();
        test.ConsumeSuccessfully();
        test.Store.Setup(x => x.StoreAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("storage failed"));

        var act = () => test.RotateAsync();
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("storage failed");

        test.Store.Verify(x => x.TryConsumeAsync(
            It.IsAny<string>(), It.IsAny<DateTimeOffset>(),
            It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        // An EF Core integration test must separately prove transaction rollback.
    }

    [Fact]
    public async Task RotateAsync_MultiTenant_UsesValidatedTenantForIssuance()
    {
        var tenant = TenantKey.FromExternal("tenant-a");
        var test = new Fixture(UAuthMode.PureJwt, tenant, multiTenant: true);
        test.Validate(test.CreateToken());
        test.IssueSuccessfully();
        test.ConsumeSuccessfully();

        TokenIssuanceContext? captured = null;
        test.Issuer.Setup(x => x.IssueAccessTokenAsync(
                It.IsAny<AuthFlowContext>(), It.IsAny<TokenIssuanceContext>(),
                It.IsAny<CancellationToken>()))
            .Callback<AuthFlowContext, TokenIssuanceContext, CancellationToken>(
                (_, context, _) => captured = context)
            .ReturnsAsync(test.AccessToken);

        (await test.RotateAsync()).Result.IsSuccess.Should().BeTrue();
        captured.Should().NotBeNull();
        captured!.Tenant.Should().Be(tenant);
        test.StoreFactory.Verify(x => x.Create(tenant), Times.Once);
    }

    [Fact]
    public async Task RotateAsync_SingleTenant_UsesSingleTenantForIssuance()
    {
        var test = new Fixture(UAuthMode.PureJwt);
        test.Validate(test.CreateToken());
        test.IssueSuccessfully();
        test.ConsumeSuccessfully();

        TokenIssuanceContext? captured = null;
        test.Issuer.Setup(x => x.IssueAccessTokenAsync(
                It.IsAny<AuthFlowContext>(), It.IsAny<TokenIssuanceContext>(),
                It.IsAny<CancellationToken>()))
            .Callback<AuthFlowContext, TokenIssuanceContext, CancellationToken>(
                (_, context, _) => captured = context)
            .ReturnsAsync(test.AccessToken);

        (await test.RotateAsync()).Result.IsSuccess.Should().BeTrue();
        captured.Should().NotBeNull();
        captured!.Tenant.Should().Be(TenantKey.Single);
    }

    [Fact]
    public async Task RotateAsync_RefreshDisabled_SkipsValidationAndStore()
    {
        var test = new Fixture(UAuthMode.PureJwt);
        test.Flow.Options.Token.IssueRefresh = false;

        AssertFailed(await test.RotateAsync());
        test.Validator.VerifyNoOtherCalls();
        test.StoreFactory.VerifyNoOtherCalls();
        test.Issuer.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RotateAsync_PureOpaque_SkipsValidationAndStore()
    {
        var test = new Fixture(UAuthMode.PureOpaque);

        AssertFailed(await test.RotateAsync());
        test.Validator.VerifyNoOtherCalls();
        test.StoreFactory.VerifyNoOtherCalls();
        test.Issuer.VerifyNoOtherCalls();
    }

    [Fact]
    public void RefreshTokenInfo_DoesNotExposeTokenHash()
    {
        var info = new RefreshTokenInfo
        {
            Token = "raw-refresh-token",
            ExpiresAt = Now.AddDays(7)
        };

        var json = JsonSerializer.Serialize(info);
        json.Should().Contain("raw-refresh-token");
        json.Should().NotContain("TokenHash");
        typeof(RefreshTokenInfo).GetProperty("TokenHash").Should().BeNull();
    }

    private static void AssertFailed(RefreshTokenRotationExecution result)
    {
        result.Result.IsSuccess.Should().BeFalse();
        result.Result.ReauthRequired.Should().BeTrue();
        result.Result.AccessToken.Should().BeNull();
        result.Result.RefreshToken.Should().BeNull();
    }

    private sealed class Fixture
    {
        public TenantKey Tenant { get; }
        public UserKey User { get; } = UserKey.New();
        public AuthFlowContext Flow { get; }
        public Mock<IRefreshTokenValidator> Validator { get; } = new();
        public Mock<IRefreshTokenStore> Store { get; } = new();
        public Mock<IRefreshTokenStoreFactory> StoreFactory { get; } = new();
        public Mock<ITokenIssuer> Issuer { get; } = new();
        public Mock<IUserClaimsProvider> Claims { get; } = new();
        public Mock<IPrimaryUserIdentifierProvider> Identifiers { get; } = new();
        public Mock<IUserProfileSnapshotProvider> Profiles { get; } = new();
        public AccessToken AccessToken { get; } = new()
        {
            Token = "access-token",
            Format = TokenFormat.Jwt,
            ExpiresAt = Now.AddMinutes(15)
        };
        public RefreshTokenIssuanceResult RefreshIssuance { get; private set; } = new()
        {
            Token = "replacement-raw",
            TokenHash = "replacement-hash",
            ExpiresAt = Now.AddDays(7)
        };

        public Fixture(UAuthMode mode, TenantKey? tenant = null, bool multiTenant = false)
        {
            Tenant = tenant ?? TenantKey.Single;
            var options = TestServerOptions.Default();
            options.MultiTenant.Enabled = multiTenant;
            options.Token.IssueRefresh = true;

            Flow = new AuthFlowContext(
                flowType: AuthFlowType.RefreshSession,
                clientProfile: UAuthClientProfile.Api,
                effectiveMode: mode,
                device: TestDevice.Default(),
                tenantKey: Tenant,
                isAuthenticated: true,
                userKey: User,
                session: null,
                options: options,
                response: new EffectiveAuthResponse(
                    sessionIdDelivery: EffectiveGrantDelivery.Disabled(GrantKind.Session),
                    accessTokenDelivery: EffectiveGrantDelivery.Disabled(GrantKind.AccessToken),
                    refreshTokenDelivery: EffectiveGrantDelivery.Disabled(GrantKind.RefreshToken),
                    redirect: EffectiveRedirectResponse.Disabled),
                primaryTokenKind: PrimaryTokenKind.AccessToken,
                returnUrlInfo: ReturnUrlInfo.None());

            StoreFactory.Setup(x => x.Create(Tenant)).Returns(Store.Object);
            Store.Setup(x => x.ExecuteAsync(
                    It.IsAny<Func<CancellationToken, Task>>(),
                    It.IsAny<CancellationToken>()))
                .Returns<Func<CancellationToken, Task>, CancellationToken>((action, ct) => action(ct));
            Store.Setup(x => x.ExecuteAsync(
                    It.IsAny<Func<CancellationToken, Task<bool>>>(),
                    It.IsAny<CancellationToken>()))
                .Returns<Func<CancellationToken, Task<bool>>, CancellationToken>((action, ct) => action(ct));
            Store.Setup(x => x.StoreAsync(It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Store.Setup(x => x.RevokeBySessionChainAsync(
                    It.IsAny<SessionChainId>(), It.IsAny<DateTimeOffset>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Store.Setup(x => x.RevokeBySessionAsync(
                    It.IsAny<AuthSessionId>(), It.IsAny<DateTimeOffset>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);
            Store.Setup(x => x.RevokeByRefreshChainAsync(
                    It.IsAny<RefreshChainId>(), It.IsAny<DateTimeOffset>(),
                    It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            SetProfile("admin", "Administrator", "UTC");
        }

        public RefreshToken CreateToken(bool? sessionFree = null)
        {
            var noSession = sessionFree ?? Flow.EffectiveMode == UAuthMode.PureJwt;
            return RefreshToken.Create(
                tokenId: TokenId.New(),
                tokenHash: "old-refresh-hash",
                tenant: Tenant,
                userKey: User,
                sessionId: noSession ? null : TestIds.Session("rotation-session"),
                sessionChainId: noSession ? null : SessionChainId.New(),
                createdAt: AuthenticatedAt,
                expiresAt: Now.AddDays(7),
                refreshChainId: RefreshChainId.New(),
                deviceId: noSession ? TestDevice.Default().DeviceId : null,
                authenticatedAt: AuthenticatedAt);
        }

        public void Validate(RefreshToken token)
            => SetValidation(RefreshTokenValidationResult.Valid(token, token.TokenHash));

        public void SetValidation(RefreshTokenValidationResult validation)
        {
            Validator.Setup(x => x.ValidateAsync(
                    It.IsAny<RefreshTokenValidationContext>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(validation);
        }

        public void SetProfile(string username, string displayName, string timeZone)
        {
            Identifiers.Setup(x => x.GetAsync(Tenant, User, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PrimaryUserIdentifiers { UserName = username });
            Profiles.Setup(x => x.GetAsync(
                    Tenant, User, ProfileKey.Default, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UserProfileSnapshot
                {
                    DisplayName = displayName,
                    TimeZone = timeZone
                });
        }

        public void IssueSuccessfully(string refreshRaw = "replacement-raw", string refreshHash = "replacement-hash")
        {
            RefreshIssuance = new RefreshTokenIssuanceResult
            {
                Token = refreshRaw,
                TokenHash = refreshHash,
                ExpiresAt = Now.AddDays(7)
            };

            Issuer.Setup(x => x.IssueRefreshTokenAsync(
                    It.IsAny<AuthFlowContext>(), It.IsAny<TokenIssuanceContext>(),
                    RefreshTokenPersistence.DoNotPersist, It.IsAny<CancellationToken>()))
                .ReturnsAsync(RefreshIssuance);
            Issuer.Setup(x => x.IssueAccessTokenAsync(
                    It.IsAny<AuthFlowContext>(), It.IsAny<TokenIssuanceContext>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(AccessToken);
        }

        public void ConsumeSuccessfully()
        {
            Store.Setup(x => x.TryConsumeAsync(
                    It.IsAny<string>(), It.IsAny<DateTimeOffset>(),
                    It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);
        }

        public Task<RefreshTokenRotationExecution> RotateAsync(AuthSessionId? expectedSessionId = null)
        {
            var service = new RefreshTokenRotationService(
                Validator.Object,
                StoreFactory.Object,
                Issuer.Object,
                Claims.Object,
                Identifiers.Object,
                Profiles.Object);

            return service.RotateAsync(
                Flow,
                new RefreshTokenRotationContext
                {
                    RefreshToken = "old-raw",
                    Now = Now,
                    Device = TestDevice.Default(),
                    ExpectedSessionId = expectedSessionId
                },
                TestContext.Current.CancellationToken);
        }

        public void AssertNoMutation()
        {
            Store.Verify(x => x.TryConsumeAsync(
                It.IsAny<string>(), It.IsAny<DateTimeOffset>(),
                It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            Store.Verify(x => x.StoreAsync(
                It.IsAny<RefreshToken>(), It.IsAny<CancellationToken>()), Times.Never);
            Store.Verify(x => x.ExecuteAsync(
                It.IsAny<Func<CancellationToken, Task<bool>>>(),
                It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
