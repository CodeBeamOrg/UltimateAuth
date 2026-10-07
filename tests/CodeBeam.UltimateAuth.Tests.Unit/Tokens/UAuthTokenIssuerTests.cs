using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public sealed class UAuthTokenIssuerTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 10, 6, 12, 0, 0, TimeSpan.Zero);

    private static readonly TenantKey Tenant =
        TenantKey.FromExternal("tenant-a");

    private const string OpaqueToken =
        "opaque-token-000000000000000000000000001";

    private const string RefreshToken =
        "refresh-token-00000000000000000000000001";

    private const string RefreshHash =
        "hashed-refresh-token";

    private const string Jwt =
        "header.payload.signature";

    private const string JwtId =
        "jwt-id-000000000000000000000000000001";

    // =====================================================================
    // Access token - PureOpaque
    // =====================================================================

    [Fact]
    public async Task IssueAccessTokenAsync_WhenModeIsPureOpaque_ShouldIssueOpaqueToken()
    {
        var fixture = CreateFixture();

        var flow = CreateFlow(
            UAuthMode.PureOpaque);

        var context = CreateTokenContext();

        var result =
            await fixture.Sut.IssueAccessTokenAsync(
                flow,
                context);

        result.Format.Should().Be(TokenFormat.Opaque);
        result.Token.Should().Be(OpaqueToken);

        fixture.OpaqueGenerator.Verify(
            x => x.Generate(),
            Times.Once);

        fixture.JwtGenerator.Verify(
            x => x.CreateToken(
                It.IsAny<UAuthJwtTokenDescriptor>()),
            Times.Never);
    }

    [Fact]
    public async Task IssueAccessTokenAsync_WhenModeIsHybrid_CurrentlyIssuesOpaqueToken()
    {
        var fixture = CreateFixture();

        var flow = CreateFlow(
            UAuthMode.Hybrid);

        var context = CreateTokenContext();

        var result =
            await fixture.Sut.IssueAccessTokenAsync(
                flow,
                context);

        // Characterizes current Hybrid behavior.
        // Production code explicitly marks JWT Hybrid as a future decision.
        result.Format.Should().Be(TokenFormat.Opaque);
        result.Token.Should().Be(OpaqueToken);

        fixture.JwtGenerator.Verify(
            x => x.CreateToken(
                It.IsAny<UAuthJwtTokenDescriptor>()),
            Times.Never);
    }

    [Fact]
    public async Task IssueAccessTokenAsync_WhenOpaque_ShouldUseConfiguredLifetime()
    {
        var fixture = CreateFixture();

        var flow = CreateFlow(
            UAuthMode.PureOpaque,
            accessTokenLifetime: TimeSpan.FromMinutes(17));

        var result =
            await fixture.Sut.IssueAccessTokenAsync(
                flow,
                CreateTokenContext());

        result.ExpiresAt.Should()
            .Be(Now.AddMinutes(17));
    }

    // =====================================================================
    // Access token - JWT modes
    // =====================================================================

    [Theory]
    [InlineData(UAuthMode.SemiHybrid)]
    [InlineData(UAuthMode.PureJwt)]
    public async Task IssueAccessTokenAsync_WhenModeUsesJwt_ShouldIssueJwt(
        UAuthMode mode)
    {
        var fixture = CreateFixture();

        var flow = CreateFlow(mode);
        var context = CreateTokenContext();

        var result =
            await fixture.Sut.IssueAccessTokenAsync(
                flow,
                context);

        result.Format.Should().Be(TokenFormat.Jwt);
        result.Token.Should().Be(Jwt);

        fixture.JwtGenerator.Verify(
            x => x.CreateToken(
                It.IsAny<UAuthJwtTokenDescriptor>()),
            Times.Once);
    }

    [Fact]
    public async Task IssueAccessTokenAsync_WhenJwt_ShouldBuildDescriptorFromContextAndOptions()
    {
        var fixture = CreateFixture();

        var flow = CreateFlow(
            UAuthMode.PureJwt,
            accessTokenLifetime: TimeSpan.FromMinutes(25),
            issuer: "https://issuer.example",
            audience: "ultimate-api",
            keyId: "key-1");

        var context = CreateTokenContext(
            claims: new Dictionary<string, string>
            {
                ["role"] = "admin",
                ["permission"] = "products.read"
            });

        UAuthJwtTokenDescriptor? captured = null;

        fixture.JwtGenerator
            .Setup(x => x.CreateToken(
                It.IsAny<UAuthJwtTokenDescriptor>()))
            .Callback<UAuthJwtTokenDescriptor>(
                descriptor => captured = descriptor)
            .Returns(Jwt);

        var result =
            await fixture.Sut.IssueAccessTokenAsync(
                flow,
                context);

        captured.Should().NotBeNull();

        captured!.Subject.Should()
            .Be(context.UserKey);

        captured.Tenant.Should()
            .Be(context.Tenant);

        captured.Issuer.Should()
            .Be("https://issuer.example");

        captured.Audience.Should()
            .Be("ultimate-api");

        captured.KeyId.Should()
            .Be("key-1");

        captured.IssuedAt.Should()
            .Be(Now);

        captured.ExpiresAt.Should()
            .Be(Now.AddMinutes(25));

        captured.Claims.Should()
            .ContainKey("role")
            .WhoseValue.Should()
            .Be("admin");

        captured.Claims.Should()
            .ContainKey("permission")
            .WhoseValue.Should()
            .Be("products.read");

        result.ExpiresAt.Should()
            .Be(captured.ExpiresAt);
    }

    [Fact]
    public async Task IssueAccessTokenAsync_WhenJwt_ShouldIncludeSubjectAndTenantClaims()
    {
        var fixture = CreateFixture();

        var flow = CreateFlow(UAuthMode.PureJwt);
        var context = CreateTokenContext();

        UAuthJwtTokenDescriptor? captured = null;

        fixture.JwtGenerator
            .Setup(x => x.CreateToken(
                It.IsAny<UAuthJwtTokenDescriptor>()))
            .Callback<UAuthJwtTokenDescriptor>(
                descriptor => captured = descriptor)
            .Returns(Jwt);

        await fixture.Sut.IssueAccessTokenAsync(
            flow,
            context);

        captured.Should().NotBeNull();

        captured!.Claims.Should()
            .ContainKey("sub");

        captured.Claims!["sub"].Should()
            .Be(context.UserKey.Value);

        captured.Claims.Should()
            .ContainKey("tenant");

        captured.Claims["tenant"].Should()
            .Be(context.Tenant);
    }

    [Fact]
    public async Task IssueAccessTokenAsync_WhenJwtAndSessionExists_ShouldIncludeSessionClaim()
    {
        var fixture = CreateFixture();

        var sessionId =
            CreateSessionId(
                "jwt-session-0000000000000000000000000001");

        var flow = CreateFlow(UAuthMode.SemiHybrid);

        var context = CreateTokenContext(
            sessionId: sessionId);

        UAuthJwtTokenDescriptor? captured = null;

        fixture.JwtGenerator
            .Setup(x => x.CreateToken(
                It.IsAny<UAuthJwtTokenDescriptor>()))
            .Callback<UAuthJwtTokenDescriptor>(
                descriptor => captured = descriptor)
            .Returns(Jwt);

        var result =
            await fixture.Sut.IssueAccessTokenAsync(
                flow,
                context);

        captured!.Claims.Should()
            .ContainKey("sid");

        captured.Claims!["sid"].Should()
            .Be(sessionId);

        result.SessionId.Should()
            .Be(sessionId.ToString());
    }

    [Fact]
    public async Task IssueAccessTokenAsync_WhenJwtAndSessionDoesNotExist_ShouldNotIncludeSessionClaim()
    {
        var fixture = CreateFixture();

        var flow = CreateFlow(UAuthMode.PureJwt);

        var context = CreateTokenContext(
            sessionId: null);

        UAuthJwtTokenDescriptor? captured = null;

        fixture.JwtGenerator
            .Setup(x => x.CreateToken(
                It.IsAny<UAuthJwtTokenDescriptor>()))
            .Callback<UAuthJwtTokenDescriptor>(
                descriptor => captured = descriptor)
            .Returns(Jwt);

        var result =
            await fixture.Sut.IssueAccessTokenAsync(
                flow,
                context);

        captured!.Claims.Should()
            .NotContainKey("sid");

        result.SessionId.Should().BeNull();
    }

    [Fact]
    public async Task IssueAccessTokenAsync_WhenJwtIdEnabled_ShouldGenerateJti()
    {
        var fixture = CreateFixture();

        var flow = CreateFlow(
            UAuthMode.PureJwt,
            addJwtIdClaim: true);

        UAuthJwtTokenDescriptor? captured = null;

        fixture.JwtGenerator
            .Setup(x => x.CreateToken(
                It.IsAny<UAuthJwtTokenDescriptor>()))
            .Callback<UAuthJwtTokenDescriptor>(
                descriptor => captured = descriptor)
            .Returns(Jwt);

        await fixture.Sut.IssueAccessTokenAsync(
            flow,
            CreateTokenContext());

        fixture.OpaqueGenerator.Verify(
            x => x.GenerateJwtId(),
            Times.Once);

        captured!.Claims.Should()
            .ContainKey("jti");

        captured.Claims!["jti"].Should()
            .Be(JwtId);
    }

    [Fact]
    public async Task IssueAccessTokenAsync_WhenJwtIdDisabled_ShouldNotGenerateJti()
    {
        var fixture = CreateFixture();

        var flow = CreateFlow(
            UAuthMode.PureJwt,
            addJwtIdClaim: false);

        UAuthJwtTokenDescriptor? captured = null;

        fixture.JwtGenerator
            .Setup(x => x.CreateToken(
                It.IsAny<UAuthJwtTokenDescriptor>()))
            .Callback<UAuthJwtTokenDescriptor>(
                descriptor => captured = descriptor)
            .Returns(Jwt);

        await fixture.Sut.IssueAccessTokenAsync(
            flow,
            CreateTokenContext());

        fixture.OpaqueGenerator.Verify(
            x => x.GenerateJwtId(),
            Times.Never);

        captured!.Claims.Should()
            .NotContainKey("jti");
    }

    // =====================================================================
    // Refresh token - guards
    // =====================================================================

    [Fact]
    public async Task IssueRefreshTokenAsync_WhenModeIsPureOpaque_ShouldReturnNull()
    {
        var fixture = CreateFixture();

        var flow = CreateFlow(
            UAuthMode.PureOpaque);

        var result =
            await fixture.Sut.IssueRefreshTokenAsync(
                flow,
                CreateTokenContext(),
                RefreshTokenPersistence.Persist);

        result.Should().BeNull();

        fixture.OpaqueGenerator.Verify(
            x => x.Generate(),
            Times.Never);

        fixture.Hasher.Verify(
            x => x.Hash(It.IsAny<string>()),
            Times.Never);

        fixture.StoreFactory.Verify(
            x => x.Create(It.IsAny<TenantKey>()),
            Times.Never);
    }

    [Theory]
    [InlineData(UAuthMode.Hybrid)]
    [InlineData(UAuthMode.SemiHybrid)]
    [InlineData(UAuthMode.PureJwt)]
    public async Task IssueRefreshTokenAsync_WhenSessionIdIsMissing_ShouldReturnNull(
        UAuthMode mode)
    {
        var fixture = CreateFixture();

        var flow = CreateFlow(mode);

        var context = CreateTokenContext(
            sessionId: null);

        var result =
            await fixture.Sut.IssueRefreshTokenAsync(
                flow,
                context,
                RefreshTokenPersistence.Persist);

        result.Should().BeNull();

        fixture.OpaqueGenerator.Verify(
            x => x.Generate(),
            Times.Never);

        fixture.Hasher.Verify(
            x => x.Hash(It.IsAny<string>()),
            Times.Never);

        fixture.StoreFactory.Verify(
            x => x.Create(It.IsAny<TenantKey>()),
            Times.Never);
    }

    // =====================================================================
    // Refresh token - generation
    // =====================================================================

    [Fact]
    public async Task IssueRefreshTokenAsync_ShouldGenerateAndHashRawToken()
    {
        var fixture = CreateFixture();
        fixture.OpaqueGenerator.Setup(x => x.Generate()).Returns(RefreshToken);

        var sessionId = CreateSessionId("refresh-session-0000000000000000000000001");

        var flow = CreateFlow(UAuthMode.Hybrid);

        var context = CreateTokenContext(sessionId: sessionId);

        var result = await fixture.Sut.IssueRefreshTokenAsync(flow, context, RefreshTokenPersistence.DoNotPersist);

        result.Should().NotBeNull();
        result!.Token.Should().Be(RefreshToken);
        result.TokenHash.Should().Be(RefreshHash);
        fixture.Hasher.Verify(x => x.Hash(RefreshToken), Times.Once);
    }

    [Fact]
    public async Task IssueRefreshTokenAsync_ShouldUseConfiguredRefreshLifetime()
    {
        var fixture = CreateFixture();

        var sessionId =
            CreateSessionId(
                "refresh-session-0000000000000000000000002");

        var flow = CreateFlow(
            UAuthMode.Hybrid,
            refreshTokenLifetime: TimeSpan.FromDays(14));

        var result =
            await fixture.Sut.IssueRefreshTokenAsync(
                flow,
                CreateTokenContext(sessionId: sessionId),
                RefreshTokenPersistence.DoNotPersist);

        result!.ExpiresAt.Should()
            .Be(Now.AddDays(14));
    }

    // =====================================================================
    // Refresh token - persistence
    // =====================================================================

    [Fact]
    public async Task IssueRefreshTokenAsync_WhenPersistenceIsPersist_ShouldStoreHashedToken()
    {
        var fixture = CreateFixture();

        var sessionId =
            CreateSessionId(
                "refresh-session-0000000000000000000000003");

        var chainId =
            SessionChainId.New();

        var flow = CreateFlow(UAuthMode.Hybrid);

        var context = CreateTokenContext(
            sessionId: sessionId,
            chainId: chainId);

        RefreshToken? captured = null;

        fixture.Store
            .Setup(x => x.StoreAsync(
                It.IsAny<RefreshToken>(),
                It.IsAny<CancellationToken>()))
            .Callback<RefreshToken, CancellationToken>(
                (token, _) => captured = token)
            .Returns(Task.CompletedTask);

        await fixture.Sut.IssueRefreshTokenAsync(
            flow,
            context,
            RefreshTokenPersistence.Persist);

        captured.Should().NotBeNull();

        captured!.TokenHash.Should()
            .Be(RefreshHash);

        captured.Tenant.Should()
            .Be(flow.Tenant);

        captured.UserKey.Should()
            .Be(context.UserKey);

        captured.SessionId.Should()
            .Be(sessionId);

        captured.ChainId.Should()
            .Be(chainId);

        captured.CreatedAt.Should()
            .Be(Now);

        captured.ExpiresAt.Should()
            .Be(Now.Add(
                flow.OriginalOptions.Token.RefreshTokenLifetime));

        fixture.StoreFactory.Verify(
            x => x.Create(flow.Tenant),
            Times.Once);

        fixture.Store.Verify(
            x => x.StoreAsync(
                It.IsAny<RefreshToken>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task IssueRefreshTokenAsync_WhenPersistenceIsDoNotPersist_ShouldNotAccessStore()
    {
        var fixture = CreateFixture();

        var sessionId =
            CreateSessionId(
                "refresh-session-0000000000000000000000004");

        var flow = CreateFlow(UAuthMode.Hybrid);

        var result =
            await fixture.Sut.IssueRefreshTokenAsync(
                flow,
                CreateTokenContext(sessionId: sessionId),
                RefreshTokenPersistence.DoNotPersist);

        result.Should().NotBeNull();

        fixture.StoreFactory.Verify(
            x => x.Create(It.IsAny<TenantKey>()),
            Times.Never);

        fixture.Store.Verify(
            x => x.StoreAsync(
                It.IsAny<RefreshToken>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task IssueRefreshTokenAsync_WhenPersisting_ShouldExecuteInsideStoreTransaction()
    {
        var fixture = CreateFixture();

        var sessionId =
            CreateSessionId(
                "refresh-session-0000000000000000000000005");

        var flow = CreateFlow(UAuthMode.Hybrid);

        await fixture.Sut.IssueRefreshTokenAsync(
            flow,
            CreateTokenContext(sessionId: sessionId),
            RefreshTokenPersistence.Persist);

        fixture.Store.Verify(
            x => x.ExecuteAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task IssueRefreshTokenAsync_ShouldPassCancellationTokenToStoreTransaction()
    {
        var fixture = CreateFixture();

        var sessionId =
            CreateSessionId(
                "refresh-session-0000000000000000000000006");

        var flow = CreateFlow(UAuthMode.Hybrid);

        using var cts =
            new CancellationTokenSource();

        await fixture.Sut.IssueRefreshTokenAsync(
            flow,
            CreateTokenContext(sessionId: sessionId),
            RefreshTokenPersistence.Persist,
            cts.Token);

        fixture.Store.Verify(
            x => x.ExecuteAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                cts.Token),
            Times.Once);
    }

    [Fact]
    public async Task IssueRefreshTokenAsync_ShouldPassTransactionTokenToStoreAsync()
    {
        var fixture = CreateFixture();

        var sessionId =
            CreateSessionId(
                "refresh-session-0000000000000000000000007");

        var flow = CreateFlow(UAuthMode.Hybrid);

        using var cts =
            new CancellationTokenSource();

        CancellationToken receivedToken = default;

        fixture.Store
            .Setup(x => x.StoreAsync(
                It.IsAny<RefreshToken>(),
                It.IsAny<CancellationToken>()))
            .Callback<RefreshToken, CancellationToken>(
                (_, ct) => receivedToken = ct)
            .Returns(Task.CompletedTask);

        await fixture.Sut.IssueRefreshTokenAsync(
            flow,
            CreateTokenContext(sessionId: sessionId),
            RefreshTokenPersistence.Persist,
            cts.Token);

        receivedToken.Should()
            .Be(cts.Token);
    }

    [Fact]
    public async Task IssueAccessTokenAsync_WhenCustomClaimsContainReservedClaims_ShouldNotOverrideFrameworkClaims()
    {
        var fixture = CreateFixture();

        var flow = CreateFlow(UAuthMode.PureJwt);

        var context = CreateTokenContext(
            claims: new Dictionary<string, string>
            {
                ["sub"] = "attacker-user",
                ["tenant"] = "attacker-tenant",
                ["role"] = "admin"
            });

        UAuthJwtTokenDescriptor? captured = null;

        fixture.JwtGenerator
            .Setup(x => x.CreateToken(
                It.IsAny<UAuthJwtTokenDescriptor>()))
            .Callback<UAuthJwtTokenDescriptor>(
                descriptor => captured = descriptor)
            .Returns(Jwt);

        await fixture.Sut.IssueAccessTokenAsync(
            flow,
            context);

        captured.Should().NotBeNull();

        // Framework-owned identity claims MUST NOT be overridable
        // by caller-provided/custom claims.
        captured!.Claims.Should()
            .ContainKey("sub");

        captured.Claims!["sub"].Should()
            .Be(context.UserKey.Value);

        captured.Claims.Should()
            .ContainKey("tenant");

        captured.Claims["tenant"].Should()
            .Be(context.Tenant);

        // Non-reserved custom claims must still flow through normally.
        captured.Claims.Should()
            .ContainKey("role");

        captured.Claims["role"].Should()
            .Be("admin");
    }

    // =====================================================================
    // Fixture
    // =====================================================================

    private static Fixture CreateFixture()
    {
        var opaqueGenerator =
            new Mock<IOpaqueTokenGenerator>(
                MockBehavior.Loose);

        var jwtGenerator =
            new Mock<IJwtTokenGenerator>(
                MockBehavior.Loose);

        var hasher =
            new Mock<ITokenHasher>(
                MockBehavior.Strict);

        var storeFactory =
            new Mock<IRefreshTokenStoreFactory>(
                MockBehavior.Strict);

        var store =
            new Mock<IRefreshTokenStore>(
                MockBehavior.Loose);

        var clock =
            new Mock<IClock>(
                MockBehavior.Strict);

        clock
            .SetupGet(x => x.UtcNow)
            .Returns(Now);

        /*
         * Generate() is used for both access and refresh tokens.
         * Individual refresh tests override this when necessary.
         */
        opaqueGenerator
            .Setup(x => x.Generate())
            .Returns(OpaqueToken);

        opaqueGenerator
            .Setup(x => x.GenerateJwtId())
            .Returns(JwtId);

        jwtGenerator
            .Setup(x => x.CreateToken(
                It.IsAny<UAuthJwtTokenDescriptor>()))
            .Returns(Jwt);

        hasher
            .Setup(x => x.Hash(It.IsAny<string>()))
            .Returns(RefreshHash);

        storeFactory
            .Setup(x => x.Create(
                It.IsAny<TenantKey>()))
            .Returns(store.Object);

        store
            .Setup(x => x.ExecuteAsync(
                It.IsAny<Func<CancellationToken, Task>>(),
                It.IsAny<CancellationToken>()))
            .Returns<Func<CancellationToken, Task>, CancellationToken>(
                (action, ct) => action(ct));

        store
            .Setup(x => x.StoreAsync(
                It.IsAny<RefreshToken>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut =
            new UAuthTokenIssuer(
                opaqueGenerator.Object,
                jwtGenerator.Object,
                hasher.Object,
                storeFactory.Object,
                clock.Object);

        return new Fixture(
            sut,
            opaqueGenerator,
            jwtGenerator,
            hasher,
            storeFactory,
            store,
            clock);
    }

    // =====================================================================
    // Context helpers
    // =====================================================================

    private static AuthFlowContext CreateFlow(
        UAuthMode mode,
        TimeSpan? accessTokenLifetime = null,
        TimeSpan? refreshTokenLifetime = null,
        string? issuer = null,
        string? audience = null,
        string? keyId = null,
        bool addJwtIdClaim = true)
    {
        /*
         * Adapt only this helper to the existing AuthFlowContext builder/helper
         * in the test project if AuthFlowContext cannot be initialized directly.
         *
         * Required state:
         * - EffectiveMode = mode
         * - Tenant = Tenant
         * - OriginalOptions.Token values below
         */

        var flow = AuthFlowTestFactory.New(
            mode: mode,
            tenant: Tenant);

        flow.OriginalOptions.Token.AccessTokenLifetime =
            accessTokenLifetime ?? TimeSpan.FromMinutes(15);

        flow.OriginalOptions.Token.RefreshTokenLifetime =
            refreshTokenLifetime ?? TimeSpan.FromDays(30);

        if (issuer is not null)
            flow.OriginalOptions.Token.Issuer = issuer;

        if (audience is not null)
            flow.OriginalOptions.Token.Audience = audience;

        flow.OriginalOptions.Token.KeyId = keyId;
        flow.OriginalOptions.Token.AddJwtIdClaim = addJwtIdClaim;

        return flow;
    }

    private static TokenIssuanceContext CreateTokenContext(
        AuthSessionId? sessionId = null,
        SessionChainId? chainId = null,
        IReadOnlyDictionary<string, string>? claims = null)
    {
        return new TokenIssuanceContext
        {
            UserKey = UserKey.New(),
            Tenant = Tenant,
            SessionId = sessionId,
            ChainId = chainId,
            Claims = claims ??
                new Dictionary<string, string>(),
            IssuedAt = Now
        };
    }

    private static AuthSessionId CreateSessionId(
        string value)
    {
        AuthSessionId.TryCreate(
                value,
                out var id)
            .Should()
            .BeTrue();

        return id;
    }

    private sealed record Fixture(
        UAuthTokenIssuer Sut,
        Mock<IOpaqueTokenGenerator> OpaqueGenerator,
        Mock<IJwtTokenGenerator> JwtGenerator,
        Mock<ITokenHasher> Hasher,
        Mock<IRefreshTokenStoreFactory> StoreFactory,
        Mock<IRefreshTokenStore> Store,
        Mock<IClock> Clock);
}
