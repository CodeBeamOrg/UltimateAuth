using AwesomeAssertions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Tests.Integration.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using CodeBeam.UltimateAuth.Server.Options;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace CodeBeam.UltimateAuth.Tests.Integration;

public sealed class LoginAccountEnumerationTests
{
    [Fact]
    public void IncludeFailureDetails_ShouldBeDisabledByDefault()
    {
        using var factory = new AuthServerFactory();

        var options = factory.Services
            .GetRequiredService<IOptions<UAuthServerOptions>>()
            .Value;

        options.Login.IncludeFailureDetails.Should().BeFalse();
    }

    [Fact]
    public async Task TryLogin_UnknownUserAndWrongPassword_ShouldHaveSamePublicFailure()
    {
        using var factory = new AuthServerFactory();

        var user = await factory.CreateLoginUserAsync(
            ct: TestContext.Current.CancellationToken);

        using var client = CreateClient(factory);

        var unknown = await TryLoginAsync(
            client,
            $"unknown-{Guid.NewGuid():N}",
            "wrong-password");

        var existing = await TryLoginAsync(
            client,
            user.Identifier,
            "wrong-password");

        AssertGenericFailure(unknown);
        AssertGenericFailure(existing);
    }

    [Fact]
    public async Task TryLogin_NewAndExistingLockout_ShouldNotExposeAccountState()
    {
        using var factory = new AuthServerFactory();

        var user = await factory.CreateLoginUserAsync(
            ct: TestContext.Current.CancellationToken);

        using var client = CreateClient(factory);

        var first = await TryLoginAsync(
            client, user.Identifier, "wrong-password-1");

        var second = await TryLoginAsync(
            client, user.Identifier, "wrong-password-2");

        // The second failure triggers the configured lockout.
        AssertGenericFailure(first);
        AssertGenericFailure(second);

        // Even a correct password must not reveal active lockout.
        var locked = await TryLoginAsync(
            client, user.Identifier, user.Secret);

        AssertGenericFailure(locked);
    }

    [Fact]
    public async Task Login_FailureRedirect_ShouldNotExposeLockoutInformation()
    {
        using var factory = new AuthServerFactory();

        var user = await factory.CreateLoginUserAsync(
            ct: TestContext.Current.CancellationToken);

        using var client = CreateClient(factory);

        var first = await LoginAsync(
            client, user.Identifier, "wrong-password-1");

        var second = await LoginAsync(
            client, user.Identifier, "wrong-password-2");

        var locked = await LoginAsync(
            client, user.Identifier, user.Secret);

        AssertGenericRedirectFailure(first);
        AssertGenericRedirectFailure(second);
        AssertGenericRedirectFailure(locked);
    }

    [Fact]
    public async Task TryLogin_ExplicitDetailedMode_ShouldPreserveFailureDetails()
    {
        using var factory = AuthServerFactory.Create(options =>
        {
            options.Login.IncludeFailureDetails = true;
        });

        var user = await factory.CreateLoginUserAsync(
            ct: TestContext.Current.CancellationToken);

        using var client = CreateClient(factory);

        var first = await TryLoginAsync(
            client, user.Identifier, "wrong-password-1");

        first.IsSuccess.Should().BeFalse();
        first.Reason.Should().Be(AuthFailureReason.InvalidCredentials);
        first.RemainingAttempts.Should().Be(1);
        first.LockoutUntilUtc.Should().BeNull();

        var second = await TryLoginAsync(
            client, user.Identifier, "wrong-password-2");

        second.IsSuccess.Should().BeFalse();
        second.Reason.Should().Be(AuthFailureReason.LockedOut);
        second.RemainingAttempts.Should().Be(0);
        second.LockoutUntilUtc.Should().NotBeNull();

        var locked = await TryLoginAsync(
            client, user.Identifier, user.Secret);

        locked.IsSuccess.Should().BeFalse();
        locked.Reason.Should().Be(AuthFailureReason.LockedOut);
        locked.RemainingAttempts.Should().Be(0);
        locked.LockoutUntilUtc.Should().Be(second.LockoutUntilUtc);
    }

    [Fact]
    public async Task PkcePreview_UnknownWrongPasswordAndLockedAccount_ShouldReturnGenericFailure()
    {
        using var factory = new AuthServerFactory();

        var user = await factory.CreateLoginUserAsync(
            ct: TestContext.Current.CancellationToken);

        using var client = CreateClient(factory);

        var unknown = await TryPkceLoginAsync(
            client,
            $"unknown-{Guid.NewGuid():N}",
            "wrong-password");

        AssertGenericPkceFailure(unknown);

        var first = await TryPkceLoginAsync(
            client,
            user.Identifier,
            "wrong-password-1");

        AssertGenericPkceFailure(first);

        // MaxFailedAttempts = 2 in the integration host.
        var second = await TryPkceLoginAsync(
            client,
            user.Identifier,
            "wrong-password-2");

        AssertGenericPkceFailure(second);

        // Correct credentials must not expose an active lockout.
        var locked = await TryPkceLoginAsync(
            client,
            user.Identifier,
            user.Secret);

        AssertGenericPkceFailure(locked);
    }

    private static void AssertGenericPkceFailure(
        TryPkceLoginResult result)
    {
        result.IsSuccess.Should().BeFalse();
        result.Reason.Should().Be(AuthFailureReason.InvalidCredentials);
        result.RemainingAttempts.Should().BeNull();
        result.LockoutUntilUtc.Should().BeNull();
        result.RequiresMfa.Should().BeFalse();
        result.RetryWithNewPkce.Should().BeFalse();
    }

    private static async Task<TryPkceLoginResult> TryPkceLoginAsync(
        HttpClient client,
        string identifier,
        string secret)
    {
        // Each attempt uses its own valid PKCE authorization.
        var verifier = WebEncoders.Base64UrlEncode(
            System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));

        var challenge = WebEncoders.Base64UrlEncode(
            System.Security.Cryptography.SHA256.HashData(
                Encoding.ASCII.GetBytes(verifier)));

        using var authorizeResponse = await CsrfTestClient.PostJsonAsync(
            client,
            "/auth/pkce/authorize",
            new
            {
                codeChallenge = challenge,
                challengeMethod = "S256",
                device = TestDevice.Default()
            });

        authorizeResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var authorization = await authorizeResponse.Content
            .ReadFromJsonAsync<PkceAuthorizeResponse>(
                cancellationToken: TestContext.Current.CancellationToken);

        authorization.Should().NotBeNull();
        authorization!.AuthorizationCode.Should().NotBeNullOrWhiteSpace();

        using var response = await CsrfTestClient.PostJsonAsync(
            client,
            "/auth/pkce/try-complete",
            new
            {
                authorization_code = authorization.AuthorizationCode,
                code_verifier = verifier,
                identifier,
                secret
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadFromJsonAsync<TryPkceLoginResult>(
                cancellationToken: TestContext.Current.CancellationToken);

        result.Should().NotBeNull();

        return result!;
    }


    private static void AssertGenericFailure(TryLoginResult result)
    {
        result.IsSuccess.Should().BeFalse();
        result.Reason.Should().Be(AuthFailureReason.InvalidCredentials);
        result.RemainingAttempts.Should().BeNull();
        result.LockoutUntilUtc.Should().BeNull();
        result.PreviewReceipt.Should().BeNullOrWhiteSpace();
    }

    private static void AssertGenericRedirectFailure(
        HttpResponseMessage response)
    {
        response.StatusCode.Should().Be(HttpStatusCode.Found);

        response.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();

        response.Headers.Location.Should().NotBeNull();

        var location = response.Headers.Location!;
        var query = QueryHelpers.ParseQuery(location.Query);

        query.ContainsKey("uauth").Should().BeTrue();

        var encoded = query["uauth"].ToString();

        var json = Encoding.UTF8.GetString(
            WebEncoders.Base64UrlDecode(encoded));

        using var document = JsonDocument.Parse(json);

        var root = document.RootElement;

        root.GetProperty("Status")
            .GetString()
            .Should().Be("failed");

        root.GetProperty("Reason")
            .GetInt32()
            .Should().Be((int)AuthFailureReason.InvalidCredentials);

        root.TryGetProperty("RemainingAttempts", out _)
            .Should().BeFalse();

        root.TryGetProperty("LockoutUntil", out _)
            .Should().BeFalse();
    }

    private static HttpClient CreateClient(AuthServerFactory factory)
    {
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = false
            });

        client.DefaultRequestHeaders.Add(
            "Origin", "https://localhost:6130");

        client.DefaultRequestHeaders.Add(
            "X-UDID", $"enumeration-{Guid.NewGuid():N}");

        return client;
    }

    private static async Task<TryLoginResult> TryLoginAsync(
        HttpClient client,
        string identifier,
        string secret)
    {
        using var response = await CsrfTestClient.PostJsonAsync(
            client,
            "/auth/try-login",
            new LoginRequest
            {
                Identifier = identifier,
                Secret = secret
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content
            .ReadFromJsonAsync<TryLoginResult>(
                cancellationToken: TestContext.Current.CancellationToken);

        result.Should().NotBeNull();
        return result!;
    }

    private static Task<HttpResponseMessage> LoginAsync(
        HttpClient client,
        string identifier,
        string secret)
    {
        return CsrfTestClient.PostJsonAsync(
            client,
            "/auth/login",
            new LoginRequest
            {
                Identifier = identifier,
                Secret = secret
            });
    }
}
