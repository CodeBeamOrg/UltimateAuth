using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Tests.Integration.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;

namespace CodeBeam.UltimateAuth.Tests.Integration;

public sealed class PkceFlowIntegrationTests : IClassFixture<AuthServerFactory>
{
    private const string AuthorizeEndpoint = "/auth/pkce/authorize";
    private const string CompleteEndpoint = "/auth/pkce/complete";
    private readonly AuthServerFactory _factory;

    public PkceFlowIntegrationTests(AuthServerFactory factory)
    {
        _factory = factory;
    }

    // ---------------------------------------------------------------------
    // Happy path
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Pkce_WithValidVerifierAndCredentials_ShouldAuthenticate()
    {
        _factory.Clock.Reset();

        var user =
            await _factory.CreateLoginUserAsync();

        using var client =
            CreateClient(
                $"pkce-valid-{Guid.NewGuid():N}");

        var verifier = CreateVerifier();
        var challenge = CreateChallenge(verifier);

        var authorization =
            await AuthorizeAsync(
                client,
                challenge);

        authorization.AuthorizationCode
            .Should().NotBeNullOrWhiteSpace();

        var response =
            await CompleteAsync(
                client,
                authorization.AuthorizationCode,
                verifier,
                user.Identifier,
                user.Secret);

        ((int)response.StatusCode)
            .Should().BeLessThan(500);

        response.Headers
            .TryGetValues("Set-Cookie", out var cookies)
            .Should().BeTrue();

        cookies.Should().NotBeNullOrEmpty();
    }

    // ---------------------------------------------------------------------
    // Consume-once / replay protection
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Pkce_AfterSuccessfulCompletion_ShouldRejectReplayOfAuthorizationCode()
    {
        _factory.Clock.Reset();

        var user =
            await _factory.CreateLoginUserAsync();

        using var client =
            CreateClient(
                $"pkce-replay-{Guid.NewGuid():N}");

        var verifier = CreateVerifier();
        var challenge = CreateChallenge(verifier);

        var authorization =
            await AuthorizeAsync(
                client,
                challenge);

        var first =
            await CompleteAsync(
                client,
                authorization.AuthorizationCode,
                verifier,
                user.Identifier,
                user.Secret);

        first.Headers
            .TryGetValues("Set-Cookie", out var firstCookies)
            .Should().BeTrue();

        firstCookies.Should().NotBeNullOrEmpty();

        // The authorization code was consumed by the first request.
        var replay =
            await CompleteAsync(
                client,
                authorization.AuthorizationCode,
                verifier,
                user.Identifier,
                user.Secret);

        replay.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();

        ((int)replay.StatusCode)
            .Should().BeLessThan(500);
    }

    [Fact]
    public async Task Pkce_WithInvalidVerifier_ShouldConsumeAuthorizationCode()
    {
        _factory.Clock.Reset();

        var user =
            await _factory.CreateLoginUserAsync();

        using var client =
            CreateClient(
                $"pkce-invalid-verifier-{Guid.NewGuid():N}");

        var correctVerifier = CreateVerifier();

        var authorization =
            await AuthorizeAsync(
                client,
                CreateChallenge(correctVerifier));

        // First attempt deliberately uses the wrong proof.
        var invalidAttempt =
            await CompleteAsync(
                client,
                authorization.AuthorizationCode,
                "definitely-wrong-verifier",
                user.Identifier,
                user.Secret);

        invalidAttempt.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();

        // Even the correct verifier must no longer work.
        // Consume-once means an attacker cannot probe a code repeatedly.
        var retryWithCorrectVerifier =
            await CompleteAsync(
                client,
                authorization.AuthorizationCode,
                correctVerifier,
                user.Identifier,
                user.Secret);

        retryWithCorrectVerifier.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();

        ((int)retryWithCorrectVerifier.StatusCode)
            .Should().BeLessThan(500);
    }

    // ---------------------------------------------------------------------
    // Expiration
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Pkce_AfterAuthorizationCodeExpires_ShouldRejectCompletion()
    {
        _factory.Clock.Reset();

        var user =
            await _factory.CreateLoginUserAsync();

        using var client =
            CreateClient(
                $"pkce-expired-{Guid.NewGuid():N}");

        var verifier = CreateVerifier();

        var authorization =
            await AuthorizeAsync(
                client,
                CreateChallenge(verifier));

        // Configure this to the actual AuthorizationCodeLifetimeSeconds
        // if your test server default differs.
        _factory.Clock.Advance(
            TimeSpan.FromMinutes(10));

        var response =
            await CompleteAsync(
                client,
                authorization.AuthorizationCode,
                verifier,
                user.Identifier,
                user.Secret);

        response.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();

        ((int)response.StatusCode)
            .Should().BeLessThan(500);
    }

    // ---------------------------------------------------------------------
    // Credential boundary
    // ---------------------------------------------------------------------

    [Fact]
    public async Task Pkce_WithValidProofButInvalidCredentials_ShouldNotAuthenticate()
    {
        _factory.Clock.Reset();

        var user =
            await _factory.CreateLoginUserAsync();

        using var client =
            CreateClient(
                $"pkce-bad-password-{Guid.NewGuid():N}");

        var verifier = CreateVerifier();

        var authorization =
            await AuthorizeAsync(
                client,
                CreateChallenge(verifier));

        var response =
            await CompleteAsync(
                client,
                authorization.AuthorizationCode,
                verifier,
                user.Identifier,
                "wrong-password");

        response.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();

        ((int)response.StatusCode)
            .Should().BeLessThan(500);
    }

    [Fact]
    public async Task Pkce_WithInvalidProof_ShouldNotAuthenticateEvenWithValidCredentials()
    {
        _factory.Clock.Reset();

        var user =
            await _factory.CreateLoginUserAsync();

        using var client =
            CreateClient(
                $"pkce-invalid-proof-{Guid.NewGuid():N}");

        var verifier = CreateVerifier();

        var authorization =
            await AuthorizeAsync(
                client,
                CreateChallenge(verifier));

        var response =
            await CompleteAsync(
                client,
                authorization.AuthorizationCode,
                "wrong-verifier",
                user.Identifier,
                user.Secret);

        response.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();

        ((int)response.StatusCode)
            .Should().BeLessThan(500);
    }

    // ---------------------------------------------------------------------
    // Context binding regression
    // ---------------------------------------------------------------------

    //[Fact]
    //public async Task Pkce_WhenCompletionReturnUrlDiffers_ShouldRejectCompletion()
    //{
    //    _factory.Clock.Reset();

    //    var user =
    //        await _factory.CreateLoginUserAsync();

    //    using var client =
    //        CreateClient(
    //            $"pkce-context-return-{Guid.NewGuid():N}");

    //    var verifier = CreateVerifier();

    //    var authorization =
    //        await AuthorizeAsync(
    //            client,
    //            CreateChallenge(verifier),
    //            returnUrl: "/original-return");

    //    var response =
    //        await CompleteAsync(
    //            client,
    //            authorization.AuthorizationCode,
    //            verifier,
    //            user.Identifier,
    //            user.Secret,
    //            returnUrl: "/different-return");

    //    // SECURITY INVARIANT:
    //    //
    //    // The PKCE artifact was issued for /original-return.
    //    // Completion is attempting to use /different-return.
    //    //
    //    // This should fail through ContextMismatch.
    //    response.Headers
    //        .TryGetValues("Set-Cookie", out _)
    //        .Should().BeFalse();
    //}

    //[Fact]
    //public async Task Pkce_WhenCompletionUsesDifferentDevice_ShouldRejectCompletion()
    //{
    //    _factory.Clock.Reset();

    //    var user =
    //        await _factory.CreateLoginUserAsync();

    //    var verifier = CreateVerifier();

    //    using var authorizationClient =
    //        CreateClient(
    //            $"pkce-device-a-{Guid.NewGuid():N}");

    //    var authorization =
    //        await AuthorizeAsync(
    //            authorizationClient,
    //            CreateChallenge(verifier));

    //    using var completionClient =
    //        CreateClient(
    //            $"pkce-device-b-{Guid.NewGuid():N}");

    //    var response =
    //        await CompleteAsync(
    //            completionClient,
    //            authorization.AuthorizationCode,
    //            verifier,
    //            user.Identifier,
    //            user.Secret);

    //    // SECURITY INVARIANT:
    //    //
    //    // PKCE authorization was initiated from device A.
    //    // Device B must not be able to complete the same flow.
    //    response.Headers
    //        .TryGetValues("Set-Cookie", out _)
    //        .Should().BeFalse();
    //}

    [Fact]
    public async Task Pkce_WithValidProofAndRepeatedInvalidCredentials_ShouldReachCredentialLockoutPipeline()
    {
        _factory.Clock.Reset();

        var user =
            await _factory.CreateLoginUserAsync();

        using var client =
            CreateClient(
                $"pkce-lockout-{Guid.NewGuid():N}");

        // -------------------------------------------------------------
        // Attempt 1
        //
        // Each PKCE authorization code is consume-once, including when
        // the subsequent credential authentication fails.
        // Therefore every credential attempt needs a fresh PKCE flow.
        // -------------------------------------------------------------

        var verifier1 = CreateVerifier();

        var authorization1 =
            await AuthorizeAsync(
                client,
                CreateChallenge(verifier1));

        var firstFailure =
            await CompleteAsync(
                client,
                authorization1.AuthorizationCode,
                verifier1,
                user.Identifier,
                "wrong-password-1");

        firstFailure.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();

        ((int)firstFailure.StatusCode)
            .Should().BeLessThan(500);

        // -------------------------------------------------------------
        // Attempt 2
        //
        // Fresh authorization code + valid PKCE proof, but another
        // invalid credential attempt.
        //
        // This must reach the normal credential pipeline and trigger
        // the configured lockout threshold.
        // -------------------------------------------------------------

        var verifier2 = CreateVerifier();

        var authorization2 =
            await AuthorizeAsync(
                client,
                CreateChallenge(verifier2));

        var secondFailure =
            await CompleteAsync(
                client,
                authorization2.AuthorizationCode,
                verifier2,
                user.Identifier,
                "wrong-password-2");

        secondFailure.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();

        ((int)secondFailure.StatusCode)
            .Should().BeLessThan(500);

        // -------------------------------------------------------------
        // Attempt 3
        //
        // Credentials are now CORRECT.
        //
        // A fresh and valid PKCE transaction must still not authenticate
        // because the credential/user security state should already be
        // locked by the previous two credential failures.
        // -------------------------------------------------------------

        var verifier3 = CreateVerifier();

        var authorization3 =
            await AuthorizeAsync(
                client,
                CreateChallenge(verifier3));

        var lockedAttempt =
            await CompleteAsync(
                client,
                authorization3.AuthorizationCode,
                verifier3,
                user.Identifier,
                user.Secret);

        lockedAttempt.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();

        ((int)lockedAttempt.StatusCode)
            .Should().BeLessThan(500);
    }

    // ---------------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------------

    private HttpClient CreateClient(
        string deviceId)
    {
        var client =
            _factory.CreateClient(
                new WebApplicationFactoryClientOptions
                {
                    AllowAutoRedirect = false,
                    HandleCookies = false
                });

        client.DefaultRequestHeaders.Add(
            "Origin",
            "https://localhost:6130");

        client.DefaultRequestHeaders.Add(
            "X-UDID",
            deviceId);

        return client;
    }

    private static async Task<PkceAuthorizeResponse> AuthorizeAsync(HttpClient client, string challenge, string? returnUrl = "/callback")
    {
        using var content = new FormUrlEncodedContent(
            new Dictionary<string, string>
            {
                ["code_challenge"] = challenge,
                ["challenge_method"] = "S256",
                ["redirect_uri"] = returnUrl ?? string.Empty
            });

        var response =
            await client.PostAsync(
                AuthorizeEndpoint,
                content);

        response.StatusCode
            .Should().Be(HttpStatusCode.OK);

        var result =
            await response.Content
                .ReadFromJsonAsync<PkceAuthorizeResponse>();

        result.Should().NotBeNull();
        result!.AuthorizationCode
            .Should().NotBeNullOrWhiteSpace();

        return result;
    }

    private static Task<HttpResponseMessage> CompleteAsync(HttpClient client, string authorizationCode, string verifier,
                                             string identifier, string secret, string? returnUrl = "/callback")
    {
        var values =
            new Dictionary<string, string>
            {
                ["authorization_code"] =
                    authorizationCode,

                ["code_verifier"] =
                    verifier,

                ["Identifier"] =
                    identifier,

                ["Secret"] =
                    secret,

                [UAuthConstants.Form.ReturnUrl] =
                    returnUrl ?? string.Empty
            };

        return client.PostAsync(
            CompleteEndpoint,
            new FormUrlEncodedContent(values));
    }

    private static string CreateVerifier()
    {
        return Convert
            .ToBase64String(
                RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private static string CreateChallenge(
        string verifier)
    {
        var hash =
            SHA256.HashData(
                Encoding.ASCII.GetBytes(verifier));

        return Convert
            .ToBase64String(hash)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }
}
