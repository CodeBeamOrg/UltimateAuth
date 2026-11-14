using System.Text;
using System.Text.Json;
using AwesomeAssertions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Server.Authentication;
using CodeBeam.UltimateAuth.Server.Endpoints;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server;

public sealed class AuthenticationLockoutEndpointHandlerTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task GetAsync_RequiresAuthentication(bool authenticated)
    {
        var f = new Fixture(authenticated);

        if (authenticated)
            return;

        var result = await f.Sut.GetAsync(f.Target, f.Http);

        result.Should().BeOfType<UnauthorizedHttpResult>();
        f.ContextFactory.VerifyNoOtherCalls();
        f.Lockouts.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAsync_UsesAdminAccessContextAndTargetUser()
    {
        var f = new Fixture();
        var request = new LockoutQueryRequest(
            AuthenticationSecurityScope.Factor,
            CredentialType.Password);

        f.SetBody(request);
        f.SetupAccess(UAuthActions.Authentication.GetLockoutAdmin);

        var expected = new AuthenticationLockoutInfo(
            AuthenticationSecurityScope.Factor,
            CredentialType.Password,
            true, 3, null, null, false, 2);

        f.Lockouts.Setup(x => x.GetAsync(
                f.Access,
                It.Is<LockoutQueryRequest>(r =>
                    r.Scope == request.Scope &&
                    r.CredentialType == request.CredentialType),
                f.Http.RequestAborted))
            .ReturnsAsync(expected);

        var result = await f.Sut.GetAsync(f.Target, f.Http);

        var ok = result.Should()
            .BeOfType<Ok<AuthenticationLockoutInfo>>()
            .Subject;

        ok.Value.Should().BeSameAs(expected);

        f.VerifyAccess(UAuthActions.Authentication.GetLockoutAdmin);

        f.Http.Response.Headers.CacheControl
            .ToString().Should().Be("no-store");
    }

    [Fact]
    public async Task UnlockAsync_UsesAdminAccessContextAndTargetUser()
    {
        var f = new Fixture();
        var request = new UnlockAuthenticationRequest(
            AuthenticationSecurityScope.Account);

        f.SetBody(request);
        f.SetupAccess(UAuthActions.Authentication.UnlockAdmin);

        var info = new AuthenticationLockoutInfo(
            AuthenticationSecurityScope.Account,
            null, false, 0, null, null, false, 3);

        var expected = new AuthenticationUnlockResult(
            AuthenticationSecurityScope.Account,
            null, info);

        f.Lockouts.Setup(x => x.UnlockAsync(
                f.Access,
                It.Is<UnlockAuthenticationRequest>(r =>
                    r.Scope == AuthenticationSecurityScope.Account &&
                    r.CredentialType == null),
                f.Http.RequestAborted))
            .ReturnsAsync(expected);

        var result = await f.Sut.UnlockAsync(f.Target, f.Http);

        var ok = result.Should()
            .BeOfType<Ok<AuthenticationUnlockResult>>()
            .Subject;

        ok.Value.Should().BeSameAs(expected);

        f.VerifyAccess(UAuthActions.Authentication.UnlockAdmin);

        f.Http.Response.Headers.CacheControl
            .ToString().Should().Be("no-store");
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task UnauthenticatedRequest_DoesNotReadBodyOrCallService(
        bool unlock)
    {
        var f = new Fixture(authenticated: false);

        var result = unlock
            ? await f.Sut.UnlockAsync(f.Target, f.Http)
            : await f.Sut.GetAsync(f.Target, f.Http);

        result.Should().BeOfType<UnauthorizedHttpResult>();

        f.ContextFactory.VerifyNoOtherCalls();
        f.Lockouts.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UnlockAsync_AccessFailure_DoesNotCallLockoutService()
    {
        var f = new Fixture();
        f.SetBody(new UnlockAuthenticationRequest(
            AuthenticationSecurityScope.Account));

        f.ContextFactory.Setup(x => x.CreateAsync(
                f.Flow,
                UAuthActions.Authentication.UnlockAdmin,
                "authentication",
                f.Target.Value,
                null,
                default))
            .ThrowsAsync(new UnauthorizedAccessException());

        var act = () => f.Sut.UnlockAsync(f.Target, f.Http);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();

        f.Lockouts.VerifyNoOtherCalls();
    }

    private sealed class Fixture
    {
        public UserKey Target { get; } = UserKey.New();
        public DefaultHttpContext Http { get; } = new();
        public AuthFlowContext Flow { get; }

        public Mock<IAuthFlowContextAccessor> FlowAccessor { get; } = new();
        public Mock<IAccessContextFactory> ContextFactory { get; } = new();
        public Mock<IAuthenticationLockoutService> Lockouts { get; } = new();

        public AccessContext Access { get; }
        public AuthenticationLockoutEndpointHandler Sut { get; }

        public Fixture(bool authenticated = true)
        {
            Flow = AuthFlowTestFactory.New(
                isAuthenticated: authenticated);

            Access = TestAccessContext.ForTargetUser(
                UserKey.New(),
                Target,
                UAuthActions.Authentication.UnlockAdmin);

            FlowAccessor.SetupGet(x => x.Current).Returns(Flow);

            Sut = new AuthenticationLockoutEndpointHandler(
                FlowAccessor.Object,
                ContextFactory.Object,
                Lockouts.Object);
        }

        public void SetBody<T>(T request)
        {
            Http.Request.ContentType = "application/json";
            Http.Request.Body = new MemoryStream(
                Encoding.UTF8.GetBytes(JsonSerializer.Serialize(request)));
        }

        public void SetupAccess(string action)
        {
            ContextFactory.Setup(x => x.CreateAsync(
                    Flow,
                    action,
                    "authentication",
                    Target.Value,
                    null,
                    default))
                .ReturnsAsync(Access);
        }

        public void VerifyAccess(string action)
        {
            ContextFactory.Verify(x => x.CreateAsync(
                    Flow,
                    action,
                    "authentication",
                    Target.Value,
                    null,
                    default),
                Times.Once);
        }
    }
}
