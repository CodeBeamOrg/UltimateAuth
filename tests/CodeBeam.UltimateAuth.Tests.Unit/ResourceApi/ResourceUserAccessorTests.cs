using System.Security.Claims;
using AwesomeAssertions;
using CodeBeam.UltimateAuth.Core;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Server.ResourceApi;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit.ResourceApi;

public sealed class ResourceUserAccessorTests
{
    private const string Scheme = UAuthConstants.SchemeDefaults.GlobalScheme;
    private const string Subject = "user-123";

    [Fact]
    public async Task ResolveAsync_PureJwt_AuthenticationFails_SetsAnonymous()
    {
        var fixture = CreateFixture(AuthenticateResult.NoResult());

        await fixture.Sut.ResolveAsync(fixture.Context);

        AssertAnonymous(fixture.Context);
        fixture.Converter.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResolveAsync_PureJwt_WithoutAuthenticatedIdentity_SetsAnonymous()
    {
        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.NameIdentifier, Subject) }));

        var fixture = CreateFixture(Success(principal));

        await fixture.Sut.ResolveAsync(fixture.Context);

        AssertAnonymous(fixture.Context);
        fixture.Converter.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResolveAsync_PureJwt_WithMultipleAuthenticatedIdentities_SetsAnonymous()
    {
        var principal = new ClaimsPrincipal(new[]
        {
            Identity(Subject),
            Identity(Subject)
        });

        var fixture = CreateFixture(Success(principal));

        await fixture.Sut.ResolveAsync(fixture.Context);

        AssertAnonymous(fixture.Context);
        fixture.Converter.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ResolveAsync_PureJwt_WithMissingOrBlankSubject_SetsAnonymous(
        string? subject)
    {
        var fixture = CreateFixture(
            Success(new ClaimsPrincipal(Identity(subject))));

        await fixture.Sut.ResolveAsync(fixture.Context);

        AssertAnonymous(fixture.Context);
        fixture.Converter.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResolveAsync_PureJwt_WithDuplicateSubjects_SetsAnonymous()
    {
        var identity = Identity(Subject);
        identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "another-user"));

        var fixture = CreateFixture(
            Success(new ClaimsPrincipal(identity)));

        await fixture.Sut.ResolveAsync(fixture.Context);

        AssertAnonymous(fixture.Context);
        fixture.Converter.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResolveAsync_PureJwt_WhenConverterThrowsFormatException_SetsAnonymous()
    {
        var fixture = CreateFixture(
            Success(new ClaimsPrincipal(Identity(Subject))));

        fixture.Converter
            .Setup(x => x.FromString(Subject))
            .Throws(new FormatException("Invalid user ID"));

        await fixture.Sut.ResolveAsync(fixture.Context);

        AssertAnonymous(fixture.Context);
    }

    [Fact]
    public async Task ResolveAsync_PureJwt_WithValidSubject_SetsAuthenticatedUserAndPrincipal()
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
    }

    private static Fixture CreateFixture(AuthenticateResult authenticationResult)
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

        var options = Options.Create(new UAuthResourceApiOptions
        {
            Auth = new UAuthResourceAuthOptions
            {
                Mode = UAuthMode.PureJwt
            }
        });

        var sut = new ResourceUserAccessor<string>(
            resolver.Object,
            options);

        return new Fixture(sut, context, converter);
    }

    private static ClaimsIdentity Identity(string? subject)
    {
        var claims = subject is null
            ? Array.Empty<Claim>()
            : new[] { new Claim(ClaimTypes.NameIdentifier, subject) };

        return new ClaimsIdentity(claims, Scheme);
    }

    private static AuthenticateResult Success(ClaimsPrincipal principal)
    {
        return AuthenticateResult.Success(
            new AuthenticationTicket(principal, Scheme));
    }

    private static AuthUserSnapshot<string> GetSnapshot(HttpContext context)
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

    private sealed record Fixture(
        ResourceUserAccessor<string> Sut,
        DefaultHttpContext Context,
        Mock<IUserIdConverter<string>> Converter);
}
