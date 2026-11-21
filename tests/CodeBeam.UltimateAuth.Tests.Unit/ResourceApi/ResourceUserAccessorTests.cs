using System.Security.Claims;
using AwesomeAssertions;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Server.ResourceApi;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit.ResourceApi;

public sealed class ResourceUserAccessorTests
{
    private const string Scheme = UAuthConstants.SchemeDefaults.GlobalScheme;
    private const string Subject = "user-123";

    [Fact]
    public async Task ResolveAsync_AuthenticationFails_SetsAnonymous()
    {
        var fixture = CreateFixture(AuthenticateResult.NoResult());

        await fixture.Sut.ResolveAsync(fixture.Context);

        AssertAnonymous(fixture.Context);
        fixture.Converter.VerifyNoOtherCalls();
        VerifyAuthenticationOnce(fixture);
    }

    [Fact]
    public async Task ResolveAsync_AuthenticationReturnsFailure_SetsAnonymous()
    {
        var fixture = CreateFixture(
            AuthenticateResult.Fail("Invalid credential"));

        await fixture.Sut.ResolveAsync(fixture.Context);

        AssertAnonymous(fixture.Context);
        fixture.Converter.VerifyNoOtherCalls();
        VerifyAuthenticationOnce(fixture);
    }

    [Fact]
    public async Task ResolveAsync_WithoutAuthenticatedIdentity_SetsAnonymous()
    {
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, Subject)]));

        var fixture = CreateFixture(Success(principal));

        await fixture.Sut.ResolveAsync(fixture.Context);

        AssertAnonymous(fixture.Context);
        fixture.Converter.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResolveAsync_WithWrongAuthenticationScheme_SetsAnonymous()
    {
        var principal = new ClaimsPrincipal(
            Identity(Subject, "AnotherScheme"));

        var fixture = CreateFixture(Success(principal));

        await fixture.Sut.ResolveAsync(fixture.Context);

        AssertAnonymous(fixture.Context);
        fixture.Converter.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResolveAsync_WithMultipleAuthenticatedIdentities_SetsAnonymous()
    {
        var principal = new ClaimsPrincipal(
        [
            Identity(Subject),
            Identity(Subject)
        ]);

        var fixture = CreateFixture(Success(principal));

        await fixture.Sut.ResolveAsync(fixture.Context);

        AssertAnonymous(fixture.Context);
        fixture.Converter.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResolveAsync_WithAdditionalUnauthenticatedIdentity_SetsAnonymous()
    {
        var principal = new ClaimsPrincipal(
        [
            Identity(Subject),
            new ClaimsIdentity()
        ]);

        var fixture = CreateFixture(Success(principal));

        await fixture.Sut.ResolveAsync(fixture.Context);

        AssertAnonymous(fixture.Context);
        fixture.Converter.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ResolveAsync_WithMissingOrBlankSubject_SetsAnonymous(
        string? subject)
    {
        var fixture = CreateFixture(
            Success(new ClaimsPrincipal(Identity(subject))));

        await fixture.Sut.ResolveAsync(fixture.Context);

        AssertAnonymous(fixture.Context);
        fixture.Converter.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResolveAsync_WithDifferentDuplicateSubjects_SetsAnonymous()
    {
        var identity = Identity(Subject);

        identity.AddClaim(
            new Claim(ClaimTypes.NameIdentifier, "another-user"));

        var fixture = CreateFixture(
            Success(new ClaimsPrincipal(identity)));

        await fixture.Sut.ResolveAsync(fixture.Context);

        AssertAnonymous(fixture.Context);
        fixture.Converter.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResolveAsync_WithIdenticalDuplicateSubjects_SetsAnonymous()
    {
        var identity = Identity(Subject);

        identity.AddClaim(
            new Claim(ClaimTypes.NameIdentifier, Subject));

        var fixture = CreateFixture(
            Success(new ClaimsPrincipal(identity)));

        await fixture.Sut.ResolveAsync(fixture.Context);

        AssertAnonymous(fixture.Context);
        fixture.Converter.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResolveAsync_WhenConverterThrowsFormatException_SetsAnonymous()
    {
        var fixture = CreateFixture(
            Success(new ClaimsPrincipal(Identity(Subject))));

        fixture.Converter
            .Setup(x => x.FromString(Subject))
            .Throws(new FormatException("Invalid user ID"));

        await fixture.Sut.ResolveAsync(fixture.Context);

        AssertAnonymous(fixture.Context);

        fixture.Converter.Verify(
            x => x.FromString(Subject),
            Times.Once);
    }

    [Fact]
    public async Task ResolveAsync_WithValidSubject_SetsAuthenticatedUserAndPrincipal()
    {
        var principal = new ClaimsPrincipal(Identity(Subject));
        var fixture = CreateFixture(Success(principal));

        fixture.Converter
            .Setup(x => x.FromString(Subject))
            .Returns("converted-user");

        await fixture.Sut.ResolveAsync(fixture.Context);

        var snapshot = GetSnapshot(fixture.Context);

        snapshot.IsAuthenticated.Should().BeTrue();
        snapshot.UserId.Should().Be("converted-user");

        fixture.Context.User.Should().BeSameAs(principal);

        fixture.Converter.Verify(
            x => x.FromString(Subject),
            Times.Once);

        VerifyAuthenticationOnce(fixture);
    }

    [Fact]
    public async Task ResolveAsync_WithValidSubject_PreservesPrincipalClaims()
    {
        var identity = Identity(Subject);
        identity.AddClaim(new Claim("uauth:permission", "users.read"));

        var principal = new ClaimsPrincipal(identity);
        var fixture = CreateFixture(Success(principal));

        fixture.Converter
            .Setup(x => x.FromString(Subject))
            .Returns("converted-user");

        await fixture.Sut.ResolveAsync(fixture.Context);

        fixture.Context.User.Should().BeSameAs(principal);

        fixture.Context.User
            .HasClaim("uauth:permission", "users.read")
            .Should().BeTrue();

        GetSnapshot(fixture.Context)
            .IsAuthenticated.Should().BeTrue();
    }

    [Fact]
    public async Task ResolveAsync_AuthenticationFails_DoesNotReplaceExistingPrincipal()
    {
        var existingPrincipal = new ClaimsPrincipal(
            new ClaimsIdentity());

        var fixture = CreateFixture(AuthenticateResult.NoResult());

        fixture.Context.User = existingPrincipal;

        await fixture.Sut.ResolveAsync(fixture.Context);

        AssertAnonymous(fixture.Context);

        fixture.Context.User.Should().BeSameAs(existingPrincipal);
    }

    private static Fixture CreateFixture(
        AuthenticateResult authenticationResult)
    {
        var authentication = new Mock<IAuthenticationService>();

        authentication
            .Setup(x => x.AuthenticateAsync(
                It.IsAny<HttpContext>(),
                Scheme))
            .ReturnsAsync(authenticationResult);

        var services = new ServiceCollection()
            .AddSingleton(authentication.Object)
            .BuildServiceProvider();

        var context = new DefaultHttpContext
        {
            RequestServices = services
        };

        var converter = new Mock<IUserIdConverter<string>>();
        var resolver = new Mock<IUserIdConverterResolver>();

        resolver
            .Setup(x => x.GetConverter<string>(It.IsAny<string?>()))
            .Returns(converter.Object);

        var sut = new ResourceUserAccessor<string>(
            resolver.Object);

        return new Fixture(
            sut,
            context,
            converter,
            authentication,
            services);
    }

    private static ClaimsIdentity Identity(
        string? subject,
        string scheme = Scheme)
    {
        var claims = subject is null
            ? Array.Empty<Claim>()
            : [new Claim(ClaimTypes.NameIdentifier, subject)];

        return new ClaimsIdentity(claims, scheme);
    }

    private static AuthenticateResult Success(
        ClaimsPrincipal principal)
    {
        return AuthenticateResult.Success(
            new AuthenticationTicket(principal, Scheme));
    }

    private static AuthUserSnapshot<string> GetSnapshot(
        HttpContext context)
    {
        return context.Items[UAuthConstants.HttpItems.UserContextKey]
            .Should()
            .BeOfType<AuthUserSnapshot<string>>()
            .Subject;
    }

    private static void AssertAnonymous(HttpContext context)
    {
        var snapshot = GetSnapshot(context);

        snapshot.IsAuthenticated.Should().BeFalse();
    }

    private static void VerifyAuthenticationOnce(Fixture fixture)
    {
        fixture.Authentication.Verify(
            x => x.AuthenticateAsync(
                fixture.Context,
                Scheme),
            Times.Once);
    }

    private sealed record Fixture(
        ResourceUserAccessor<string> Sut,
        DefaultHttpContext Context,
        Mock<IUserIdConverter<string>> Converter,
        Mock<IAuthenticationService> Authentication,
        ServiceProvider Services);
}
