using System.Text;
using System.Text.Json;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Credentials.Contracts;
using CodeBeam.UltimateAuth.Credentials.Reference;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public sealed class CredentialEndpointHandlerTests
{
    // =========================================================
    // GetAll - Self
    // =========================================================

    [Fact]
    public async Task GetAllAsync_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var f = new Fixture(isAuthenticated: false);

        var result = await f.Sut.GetAllAsync(f.HttpContext);

        result.Should().BeOfType<UnauthorizedHttpResult>();

        f.AccessContextFactory.VerifyNoOtherCalls();
        f.Credentials.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAllAsync_WhenAuthenticated_UsesListSelfAndReturnsOk()
    {
        var f = new Fixture();
        var access = f.SelfAccess(UAuthActions.Credentials.ListSelf);

        f.SetupAccess(
            UAuthActions.Credentials.ListSelf,
            f.UserKey.Value,
            access);

        var serviceResult = new GetCredentialsResult();

        f.Credentials
            .Setup(x => x.GetAllAsync(
                access,
                f.HttpContext.RequestAborted))
            .ReturnsAsync(serviceResult);

        var result = await f.Sut.GetAllAsync(f.HttpContext);

        var ok = result.Should()
            .BeOfType<Ok<GetCredentialsResult>>()
            .Subject;

        ok.Value.Should().BeSameAs(serviceResult);
    }

    // =========================================================
    // Add - Self
    // =========================================================

    [Fact]
    public async Task AddAsync_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var f = new Fixture(isAuthenticated: false);

        f.SetJsonBody(new AddCredentialRequest
        {
            Type = CredentialType.Password,
            Secret = "new-password"
        });

        var result = await f.Sut.AddAsync(f.HttpContext);

        result.Should().BeOfType<UnauthorizedHttpResult>();

        f.AccessContextFactory.VerifyNoOtherCalls();
        f.Credentials.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task AddAsync_WhenAuthenticated_ForwardsRequestUsingAddSelf()
    {
        var f = new Fixture();
        var access = f.SelfAccess(UAuthActions.Credentials.AddSelf);

        f.SetJsonBody(new AddCredentialRequest
        {
            Type = CredentialType.Password,
            Secret = "new-password",
            Source = "test"
        });

        f.SetupAccess(
            UAuthActions.Credentials.AddSelf,
            f.UserKey.Value,
            access);

        var serviceResult = AddCredentialResult.Success(
            Guid.NewGuid(),
            CredentialType.Password);

        f.Credentials
            .Setup(x => x.AddAsync(
                access,
                It.Is<AddCredentialRequest>(r =>
                    r.Type == CredentialType.Password &&
                    r.Secret == "new-password" &&
                    r.Source == "test"),
                f.HttpContext.RequestAborted))
            .ReturnsAsync(serviceResult);

        var result = await f.Sut.AddAsync(f.HttpContext);

        var ok = result.Should()
            .BeOfType<Ok<AddCredentialResult>>()
            .Subject;

        ok.Value.Should().BeSameAs(serviceResult);
    }

    // =========================================================
    // Change Secret - Self
    // =========================================================

    [Fact]
    public async Task ChangeSecretAsync_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var f = new Fixture(isAuthenticated: false);

        f.SetJsonBody(new ChangeCredentialRequest
        {
            CurrentSecret = "old",
            NewSecret = "new"
        });

        var result = await f.Sut.ChangeSecretAsync(f.HttpContext);

        result.Should().BeOfType<UnauthorizedHttpResult>();

        f.AccessContextFactory.VerifyNoOtherCalls();
        f.Credentials.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ChangeSecretAsync_WhenAuthenticated_ForwardsRequestUsingChangeSelf()
    {
        var f = new Fixture();
        var access = f.SelfAccess(UAuthActions.Credentials.ChangeSelf);

        f.SetJsonBody(new ChangeCredentialRequest
        {
            CurrentSecret = "old-password",
            NewSecret = "new-password"
        });

        f.SetupAccess(
            UAuthActions.Credentials.ChangeSelf,
            f.UserKey.Value,
            access);

        var serviceResult =
            ChangeCredentialResult.Success(CredentialType.Password);

        f.Credentials
            .Setup(x => x.ChangeSecretAsync(
                access,
                It.Is<ChangeCredentialRequest>(r =>
                    r.CurrentSecret == "old-password" &&
                    r.NewSecret == "new-password"),
                f.HttpContext.RequestAborted))
            .ReturnsAsync(serviceResult);

        var result = await f.Sut.ChangeSecretAsync(f.HttpContext);

        var ok = result.Should()
            .BeOfType<Ok<ChangeCredentialResult>>()
            .Subject;

        ok.Value.Should().BeSameAs(serviceResult);
    }

    // =========================================================
    // Revoke - Self
    // =========================================================

    [Fact]
    public async Task RevokeAsync_WhenAuthenticated_UsesRevokeSelfAndReturnsNoContent()
    {
        var f = new Fixture();
        var access = f.SelfAccess(UAuthActions.Credentials.RevokeSelf);
        var credentialId = Guid.NewGuid();

        f.SetJsonBody(new RevokeCredentialRequest
        {
            Id = credentialId
        });

        f.SetupAccess(
            UAuthActions.Credentials.RevokeSelf,
            f.UserKey.Value,
            access);

        f.Credentials
            .Setup(x => x.RevokeAsync(
                access,
                It.Is<RevokeCredentialRequest>(r =>
                    r.Id == credentialId),
                f.HttpContext.RequestAborted))
            .ReturnsAsync(CredentialActionResult.Success());

        var result = await f.Sut.RevokeAsync(f.HttpContext);

        result.Should().BeOfType<NoContent>();
    }

    // =========================================================
    // Begin Reset - Anonymous
    // =========================================================

    [Fact]
    public async Task BeginResetAsync_WhenUnauthenticated_IsAllowed()
    {
        var f = new Fixture(isAuthenticated: false);

        const string identifier = "alice@example.com";

        f.SetJsonBody(new BeginResetCredentialRequest
        {
            Identifier = identifier,
            CredentialType = CredentialType.Password,
            ResetCodeType = ResetCodeType.Token
        });

        var access = TestAccessContext.WithAction(
            UAuthActions.Credentials.BeginResetAnonymous);

        f.SetupAccess(
            UAuthActions.Credentials.BeginResetAnonymous,
            identifier,
            access);

        var serviceResult = new BeginCredentialResetResult();

        f.Credentials
            .Setup(x => x.BeginResetAsync(
                access,
                It.Is<BeginResetCredentialRequest>(r =>
                    r.Identifier == identifier &&
                    r.CredentialType == CredentialType.Password &&
                    r.ResetCodeType == ResetCodeType.Token),
                f.HttpContext.RequestAborted))
            .ReturnsAsync(serviceResult);

        var result = await f.Sut.BeginResetAsync(f.HttpContext);

        var ok = result.Should()
            .BeOfType<Ok<BeginCredentialResetResult>>()
            .Subject;

        ok.Value.Should().BeSameAs(serviceResult);
    }

    // =========================================================
    // Complete Reset - Anonymous
    // =========================================================

    [Fact]
    public async Task CompleteResetAsync_WhenUnauthenticated_IsAllowed()
    {
        var f = new Fixture(isAuthenticated: false);

        const string identifier = "alice@example.com";

        f.SetJsonBody(new CompleteResetCredentialRequest
        {
            Identifier = identifier,
            CredentialType = CredentialType.Password,
            ResetToken = "reset-token",
            NewSecret = "new-password"
        });

        var access = TestAccessContext.WithAction(
            UAuthActions.Credentials.CompleteResetAnonymous);

        f.SetupAccess(
            UAuthActions.Credentials.CompleteResetAnonymous,
            identifier,
            access);

        f.Credentials
            .Setup(x => x.CompleteResetAsync(
                access,
                It.Is<CompleteResetCredentialRequest>(r =>
                    r.Identifier == identifier &&
                    r.ResetToken == "reset-token" &&
                    r.NewSecret == "new-password"),
                f.HttpContext.RequestAborted))
            .ReturnsAsync(CredentialActionResult.Success());

        var result = await f.Sut.CompleteResetAsync(f.HttpContext);

        var ok = result.Should()
            .BeOfType<Ok<CredentialActionResult>>()
            .Subject;

        ok.Value!.Succeeded.Should().BeTrue();
    }

    // =========================================================
    // GetAll - Admin
    // =========================================================

    [Fact]
    public async Task GetAllAdminAsync_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var f = new Fixture(isAuthenticated: false);

        var result = await f.Sut.GetAllAdminAsync(
            UserKey.New(),
            f.HttpContext);

        result.Should().BeOfType<UnauthorizedHttpResult>();

        f.AccessContextFactory.VerifyNoOtherCalls();
        f.Credentials.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAllAdminAsync_WhenAuthenticated_UsesListAdminForTargetUser()
    {
        var f = new Fixture();
        var target = UserKey.New();

        var access = f.AdminAccess(
            target,
            UAuthActions.Credentials.ListAdmin);

        f.SetupAccess(
            UAuthActions.Credentials.ListAdmin,
            target.Value,
            access);

        var serviceResult = new GetCredentialsResult();

        f.Credentials
            .Setup(x => x.GetAllAsync(
                access,
                f.HttpContext.RequestAborted))
            .ReturnsAsync(serviceResult);

        var result = await f.Sut.GetAllAdminAsync(
            target,
            f.HttpContext);

        var ok = result.Should()
            .BeOfType<Ok<GetCredentialsResult>>()
            .Subject;

        ok.Value.Should().BeSameAs(serviceResult);
    }

    // =========================================================
    // Add - Admin
    // =========================================================

    [Fact]
    public async Task AddAdminAsync_WhenAuthenticated_UsesAddAdminForTargetUser()
    {
        var f = new Fixture();
        var target = UserKey.New();

        var access = f.AdminAccess(
            target,
            UAuthActions.Credentials.AddAdmin);

        f.SetJsonBody(new AddCredentialRequest
        {
            Type = CredentialType.Password,
            Secret = "admin-set-password"
        });

        f.SetupAccess(
            UAuthActions.Credentials.AddAdmin,
            target.Value,
            access);

        var serviceResult = AddCredentialResult.Success(
            Guid.NewGuid(),
            CredentialType.Password);

        f.Credentials
            .Setup(x => x.AddAsync(
                access,
                It.Is<AddCredentialRequest>(r =>
                    r.Secret == "admin-set-password"),
                f.HttpContext.RequestAborted))
            .ReturnsAsync(serviceResult);

        var result = await f.Sut.AddAdminAsync(
            target,
            f.HttpContext);

        result.Should().BeOfType<Ok<AddCredentialResult>>();
    }

    // =========================================================
    // Change Secret - Admin
    // =========================================================

    [Fact]
    public async Task ChangeSecretAdminAsync_WhenUnauthenticated_ReturnsUnauthorized()
    {
        var f = new Fixture(isAuthenticated: false);

        var result = await f.Sut.ChangeSecretAdminAsync(
            UserKey.New(),
            f.HttpContext);

        result.Should().BeOfType<UnauthorizedHttpResult>();

        f.AccessContextFactory.VerifyNoOtherCalls();
        f.Credentials.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ChangeSecretAdminAsync_WhenAuthenticated_UsesChangeAdminForTargetUser()
    {
        var f = new Fixture();
        var target = UserKey.New();

        var access = f.AdminAccess(
            target,
            UAuthActions.Credentials.ChangeAdmin);

        f.SetJsonBody(new ChangeCredentialRequest
        {
            NewSecret = "new-admin-password"
        });

        f.SetupAccess(
            UAuthActions.Credentials.ChangeAdmin,
            target.Value,
            access);

        var serviceResult =
            ChangeCredentialResult.Success(CredentialType.Password);

        f.Credentials
            .Setup(x => x.ChangeSecretAsync(
                access,
                It.Is<ChangeCredentialRequest>(r =>
                    r.NewSecret == "new-admin-password"),
                f.HttpContext.RequestAborted))
            .ReturnsAsync(serviceResult);

        var result = await f.Sut.ChangeSecretAdminAsync(
            target,
            f.HttpContext);

        result.Should().BeOfType<Ok<ChangeCredentialResult>>();
    }

    // =========================================================
    // Revoke - Admin
    // =========================================================

    [Fact]
    public async Task RevokeAdminAsync_WhenAuthenticated_UsesRevokeAdminAndReturnsNoContent()
    {
        var f = new Fixture();
        var target = UserKey.New();
        var credentialId = Guid.NewGuid();

        var access = f.AdminAccess(
            target,
            UAuthActions.Credentials.RevokeAdmin);

        f.SetJsonBody(new RevokeCredentialRequest
        {
            Id = credentialId
        });

        f.SetupAccess(
            UAuthActions.Credentials.RevokeAdmin,
            target.Value,
            access);

        f.Credentials
            .Setup(x => x.RevokeAsync(
                access,
                It.Is<RevokeCredentialRequest>(r =>
                    r.Id == credentialId),
                f.HttpContext.RequestAborted))
            .ReturnsAsync(CredentialActionResult.Success());

        var result = await f.Sut.RevokeAdminAsync(
            target,
            f.HttpContext);

        result.Should().BeOfType<NoContent>();
    }

    // =========================================================
    // Delete - Admin
    // =========================================================

    [Theory]
    [InlineData(DeleteMode.Soft)]
    [InlineData(DeleteMode.Hard)]
    public async Task DeleteAdminAsync_WhenAuthenticated_ForwardsDeleteModeAndReturnsNoContent(
        DeleteMode mode)
    {
        var f = new Fixture();
        var target = UserKey.New();
        var credentialId = Guid.NewGuid();

        var access = f.AdminAccess(
            target,
            UAuthActions.Credentials.DeleteAdmin);

        f.SetJsonBody(new DeleteCredentialRequest
        {
            Id = credentialId,
            Mode = mode
        });

        f.SetupAccess(
            UAuthActions.Credentials.DeleteAdmin,
            target.Value,
            access);

        f.Credentials
            .Setup(x => x.DeleteAsync(
                access,
                It.Is<DeleteCredentialRequest>(r =>
                    r.Id == credentialId &&
                    r.Mode == mode),
                f.HttpContext.RequestAborted))
            .ReturnsAsync(CredentialActionResult.Success());

        var result = await f.Sut.DeleteAdminAsync(
            target,
            f.HttpContext);

        result.Should().BeOfType<NoContent>();
    }

    // =========================================================
    // Begin Reset - Admin
    // =========================================================

    [Fact]
    public async Task BeginResetAdminAsync_WhenAuthenticated_UsesTargetUserAsResourceId()
    {
        var f = new Fixture();
        var target = UserKey.New();

        var access = f.AdminAccess(
            target,
            UAuthActions.Credentials.BeginResetAdmin);

        f.SetJsonBody(new BeginResetCredentialRequest
        {
            // Important:
            // Admin endpoint's authorization target is userKey,
            // not this identifier.
            Identifier = "alice@example.com",
            CredentialType = CredentialType.Password,
            ResetCodeType = ResetCodeType.Token
        });

        f.SetupAccess(
            UAuthActions.Credentials.BeginResetAdmin,
            target.Value,
            access);

        f.Credentials
            .Setup(x => x.BeginResetAsync(
                access,
                It.IsAny<BeginResetCredentialRequest>(),
                f.HttpContext.RequestAborted))
            .ReturnsAsync(new BeginCredentialResetResult());

        var result = await f.Sut.BeginResetAdminAsync(
            target,
            f.HttpContext);

        result.Should().BeOfType<NoContent>();
    }

    // =========================================================
    // Complete Reset - Admin
    // =========================================================

    [Fact]
    public async Task CompleteResetAdminAsync_WhenAuthenticated_UsesTargetUserAsResourceId()
    {
        var f = new Fixture();
        var target = UserKey.New();

        var access = f.AdminAccess(
            target,
            UAuthActions.Credentials.CompleteResetAdmin);

        f.SetJsonBody(new CompleteResetCredentialRequest
        {
            Identifier = "alice@example.com",
            CredentialType = CredentialType.Password,
            ResetToken = "token",
            NewSecret = "new-password"
        });

        f.SetupAccess(
            UAuthActions.Credentials.CompleteResetAdmin,
            target.Value,
            access);

        f.Credentials
            .Setup(x => x.CompleteResetAsync(
                access,
                It.IsAny<CompleteResetCredentialRequest>(),
                f.HttpContext.RequestAborted))
            .ReturnsAsync(CredentialActionResult.Success());

        var result = await f.Sut.CompleteResetAdminAsync(
            target,
            f.HttpContext);

        result.Should().BeOfType<NoContent>();
    }

    // =========================================================
    // Fixture
    // =========================================================

    private sealed class Fixture
    {
        public Mock<IAuthFlowContextAccessor> AuthFlow { get; }
            = new(MockBehavior.Strict);

        public Mock<IAccessContextFactory> AccessContextFactory { get; }
            = new(MockBehavior.Strict);

        public Mock<ICredentialManagementService> Credentials { get; }
            = new(MockBehavior.Strict);

        public DefaultHttpContext HttpContext { get; } = new();

        public UserKey UserKey { get; } = UserKey.New();

        public AuthFlowContext Flow { get; }

        public CredentialEndpointHandler Sut { get; }

        public Fixture(bool isAuthenticated = true)
        {
            Flow = AuthFlowTestFactory.New(
                isAuthenticated: isAuthenticated,
                userKey: isAuthenticated ? UserKey : null);

            AuthFlow
                .SetupGet(x => x.Current)
                .Returns(Flow);

            Sut = new CredentialEndpointHandler(
                AuthFlow.Object,
                AccessContextFactory.Object,
                Credentials.Object);
        }

        public AccessContext SelfAccess(string action)
            => TestAccessContext.ForUser(
                UserKey,
                action,
                resource: "credentials");

        public AccessContext AdminAccess(
            UserKey target,
            string action)
            => TestAccessContext.ForTargetUser(
                UserKey,
                target,
                action,
                resource: "credentials");

        public void SetupAccess(
            string action,
            string resourceId,
            AccessContext result)
        {
            AccessContextFactory
                .Setup(x => x.CreateAsync(
                    Flow,
                    action,
                    "credentials",
                    resourceId,
                    null,
                    default))
                .ReturnsAsync(result);
        }

        public void SetJsonBody<T>(T value)
        {
            var json = JsonSerializer.Serialize(
                value,
                new JsonSerializerOptions(JsonSerializerDefaults.Web));

            var bytes = Encoding.UTF8.GetBytes(json);

            HttpContext.Request.Body =
                new MemoryStream(bytes);

            HttpContext.Request.ContentType =
                "application/json";

            HttpContext.Request.ContentLength =
                bytes.Length;
        }
    }
}
