using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Users.Contracts;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

namespace CodeBeam.UltimateAuth.Tests.Integration;

public class LoginTests : IClassFixture<AuthServerFactory>
{
    private const string LoginEndpoint = "/auth/login";
    private const string ProfileEndpoint = "/auth/me/profile/get";

    private const string ValidIdentifier = "admin";
    private const string ValidSecret = "admin";

    private readonly AuthServerFactory _factory;

    public LoginTests(AuthServerFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldIssueSessionCredential()
    {
        using var client = CreateClient();

        var response = await LoginAsync(
            client,
            ValidIdentifier,
            ValidSecret);

        response.StatusCode.Should().Be(HttpStatusCode.Found);
        response.Headers.Location.Should().NotBeNull();

        response.Headers
            .TryGetValues("Set-Cookie", out var cookies)
            .Should().BeTrue();

        cookies.Should().NotBeNullOrEmpty();

        var cookie = cookies!.First();

        cookie.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Login_WithValidCredentials_ShouldCreateUsableAuthenticatedSession()
    {
        var user = await _factory.CreateLoginUserAsync();

        var deviceId = $"usable-session-{Guid.NewGuid():N}";

        using var client = CreateClient(deviceId);

        var loginResponse = await LoginAsync(
            client,
            user.Identifier,
            user.Secret);

        loginResponse.StatusCode.Should().Be(HttpStatusCode.Found);

        var cookie = GetSessionCookie(loginResponse);

        using var authenticatedClient = CreateClient(deviceId);

        authenticatedClient.DefaultRequestHeaders.Add(
            "Cookie",
            cookie);

        var response = await authenticatedClient.PostAsJsonAsync(
            "/auth/me/sessions/chains",
            new PageRequest
            {
                PageNumber = 1,
                PageSize = 10
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result =
            await response.Content.ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        result.Should().NotBeNull();
        result!.Items.Should().NotBeEmpty();

        var currentChain = result.Items.Single(x => x.IsCurrentDevice);

        currentChain.ActiveSessionId.Should().NotBeNull();
        currentChain.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldNotAuthenticateUser()
    {
        using var client = CreateClient();

        var response = await LoginAsync(
            client,
            ValidIdentifier,
            "wrong-password");

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Found);

        response.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();
    }

    [Fact]
    public async Task Login_WithUnknownIdentifier_ShouldNotAuthenticateUser()
    {
        using var client = CreateClient();

        var response = await LoginAsync(
            client,
            "unknown-user",
            ValidSecret);

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Found);

        response.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();
    }

    [Theory]
    [InlineData("", "admin")]
    [InlineData(" ", "admin")]
    [InlineData("admin", "")]
    [InlineData("admin", " ")]
    public async Task Login_WithMissingCredentials_ShouldNotAuthenticateUser(
        string identifier,
        string secret)
    {
        using var client = CreateClient();

        var response = await LoginAsync(
            client,
            identifier,
            secret);

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Found);

        response.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();
    }

    [Fact]
    public async Task Login_WithUnsupportedContentType_ShouldNotAuthenticateUser()
    {
        using var client = CreateClient();

        using var content = new StringContent(
            "identifier=admin&secret=admin");

        var response = await client.PostAsync(
            LoginEndpoint,
            content);

        response.StatusCode.Should().BeOneOf(
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Found);

        response.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();
    }

    [Fact]
    public async Task Login_WithInvalidPassword_ShouldNotIssueSessionCredential()
    {
        using var client = CreateClient();

        var response = await LoginAsync(
            client,
            ValidIdentifier,
            "definitely-wrong-password");

        response.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();
    }

    [Fact]
    public async Task Login_WithUnknownIdentifier_ShouldBehaveLikeInvalidPassword()
    {
        using var invalidPasswordClient = CreateClient(
            "enumeration-device-1111111111111111");

        using var unknownUserClient = CreateClient(
            "enumeration-device-2222222222222222");

        var invalidPasswordResponse = await LoginAsync(
            invalidPasswordClient,
            ValidIdentifier,
            "definitely-wrong-password");

        var unknownUserResponse = await LoginAsync(
            unknownUserClient,
            "user-that-does-not-exist",
            "definitely-wrong-password");

        invalidPasswordResponse.StatusCode
            .Should()
            .Be(unknownUserResponse.StatusCode);

        invalidPasswordResponse.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();

        unknownUserResponse.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();
    }

    [Fact]
    public async Task Login_AfterMaximumFailedAttempts_ShouldRejectCorrectPassword()
    {
        using var client = CreateClient(
            "lockout-device-111111111111111111");

        var firstFailure = await LoginAsync(
            client,
            ValidIdentifier,
            "wrong-password-1");

        firstFailure.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();

        var secondFailure = await LoginAsync(
            client,
            ValidIdentifier,
            "wrong-password-2");

        secondFailure.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();

        var correctPasswordDuringLockout = await LoginAsync(
            client,
            ValidIdentifier,
            ValidSecret);

        correctPasswordDuringLockout.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();

        correctPasswordDuringLockout.StatusCode.Should().BeOneOf(
            HttpStatusCode.Unauthorized,
            HttpStatusCode.Found);
    }

    [Fact]
    public async Task Login_AfterLockoutExpires_ShouldAllowCorrectCredentials()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();

        using var client = CreateClient(
            $"lockout-expiry-{Guid.NewGuid():N}");

        await LoginAsync(
            client,
            user.Identifier,
            "wrong-password-1");

        await LoginAsync(
            client,
            user.Identifier,
            "wrong-password-2");

        var duringLockout = await LoginAsync(
            client,
            user.Identifier,
            user.Secret);

        duringLockout.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();

        _factory.Clock.Advance(
            TimeSpan.FromSeconds(11));

        var afterLockout = await LoginAsync(
            client,
            user.Identifier,
            user.Secret);

        afterLockout.StatusCode.Should()
            .Be(HttpStatusCode.Found);

        afterLockout.Headers
            .TryGetValues("Set-Cookie", out var cookies)
            .Should().BeTrue();

        cookies.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task TryLogin_WithValidCredentials_ShouldReturnSuccessfulPreview()
    {
        using var client = CreateClient(
            "try-login-device-1111111111111111");

        var response = await client.PostAsJsonAsync("/auth/try-login", new
        {
            identifier = ValidIdentifier,
            secret = ValidSecret
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<TryLoginResult>();

        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.Reason.Should().BeNull();
        result.PreviewReceipt.Should().NotBeNullOrWhiteSpace();

        response.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();
    }

    [Fact]
    public async Task TryLogin_WithValidCredentials_ShouldNotAuthenticateUser()
    {
        using var client = CreateClient(
            "try-login-device-2222222222222222");

        var response = await client.PostAsJsonAsync("/auth/try-login", new
        {
            identifier = ValidIdentifier,
            secret = ValidSecret
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<TryLoginResult>();

        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();

        var meResponse = await client.PostAsJsonAsync(
            "/auth/me/profile/get",
            new GetProfileRequest
            {
                ProfileKey = null
            });

        meResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TryLogin_WithInvalidCredentials_ShouldReturnFailedPreviewWithoutSession()
    {
        using var client = CreateClient(
            "try-login-device-3333333333333333");

        var response = await client.PostAsJsonAsync("/auth/try-login", new
        {
            identifier = ValidIdentifier,
            secret = "wrong-password"
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<TryLoginResult>();

        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Reason.Should().Be(AuthFailureReason.InvalidCredentials);
        result.PreviewReceipt.Should().BeNull();

        response.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();
    }

    [Fact]
    public async Task TryLogin_WithMissingCredentials_ShouldReturnFailedPreview()
    {
        using var client = CreateClient(
            "try-login-device-4444444444444444");

        var response = await client.PostAsJsonAsync("/auth/try-login", new
        {
            identifier = "",
            secret = ""
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<TryLoginResult>();

        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Reason.Should().Be(AuthFailureReason.InvalidCredentials);
        result.PreviewReceipt.Should().BeNull();
    }

    [Fact]
    public async Task Login_WithValidPreviewReceipt_ShouldAuthenticateUser()
    {
        using var client = CreateClient(
            "try-commit-device-111111111111111");

        var previewResponse = await client.PostAsJsonAsync("/auth/try-login", new
        {
            identifier = ValidIdentifier,
            secret = ValidSecret
        });

        var preview = await previewResponse.Content
            .ReadFromJsonAsync<TryLoginResult>();

        preview.Should().NotBeNull();
        preview!.Success.Should().BeTrue();
        preview.PreviewReceipt.Should().NotBeNullOrWhiteSpace();

        var loginResponse = await client.PostAsJsonAsync("/auth/login", new
        {
            identifier = ValidIdentifier,
            secret = ValidSecret,
            previewReceipt = preview.PreviewReceipt
        });

        loginResponse.StatusCode.Should().Be(HttpStatusCode.Found);

        loginResponse.Headers
            .TryGetValues("Set-Cookie", out var cookies)
            .Should().BeTrue();

        cookies.Should().NotBeNullOrEmpty();

        using var authenticatedClient = CreateClient(
            "try-commit-device-111111111111111");

        authenticatedClient.DefaultRequestHeaders.Add(
            "Cookie",
            cookies!.First());

        var meResponse = await authenticatedClient.PostAsJsonAsync(
            "/auth/me/profile/get",
            new GetProfileRequest
            {
                ProfileKey = null
            });

        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Login_WithPreviewReceiptFromDifferentDevice_ShouldNotTrustReceipt()
    {
        var user = await _factory.CreateLoginUserAsync();

        using var previewClient = CreateClient(
            $"receipt-owner-{Guid.NewGuid():N}");

        var previewResponse = await previewClient.PostAsJsonAsync(
            "/auth/try-login",
            new
            {
                identifier = user.Identifier,
                secret = user.Secret
            });

        previewResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var preview = await previewResponse.Content
            .ReadFromJsonAsync<TryLoginResult>();

        preview.Should().NotBeNull();
        preview!.Success.Should().BeTrue();
        preview.PreviewReceipt.Should().NotBeNullOrWhiteSpace();

        // Attempt to use the valid receipt from another device.
        using var attackerClient = CreateClient(
            $"receipt-attacker-{Guid.NewGuid():N}");

        var response = await attackerClient.PostAsJsonAsync(
            "/auth/login",
            new
            {
                identifier = user.Identifier,
                secret = "wrong-password",
                previewReceipt = preview.PreviewReceipt
            });

        response.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();
    }

    [Fact]
    public async Task Login_WithPreviewReceiptAndDifferentSecret_ShouldNotAuthenticate()
    {
        using var client = CreateClient(
            "receipt-secret-device-11111111111");

        var previewResponse = await client.PostAsJsonAsync(
            "/auth/try-login",
            new
            {
                identifier = ValidIdentifier,
                secret = ValidSecret
            });

        var preview = await previewResponse.Content
            .ReadFromJsonAsync<TryLoginResult>();

        preview.Should().NotBeNull();
        preview!.PreviewReceipt.Should().NotBeNullOrWhiteSpace();

        var response = await client.PostAsJsonAsync(
            "/auth/login",
            new
            {
                identifier = ValidIdentifier,
                secret = "different-password",
                previewReceipt = preview.PreviewReceipt
            });

        response.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();
    }

    [Fact]
    public async Task Login_WithPreviewReceiptAndDifferentIdentifier_ShouldNotAuthenticate()
    {
        using var client = CreateClient(
            "receipt-identifier-device-111111");

        var previewResponse = await client.PostAsJsonAsync(
            "/auth/try-login",
            new
            {
                identifier = ValidIdentifier,
                secret = ValidSecret
            });

        var preview = await previewResponse.Content
            .ReadFromJsonAsync<TryLoginResult>();

        preview.Should().NotBeNull();
        preview!.PreviewReceipt.Should().NotBeNullOrWhiteSpace();

        var response = await client.PostAsJsonAsync(
            "/auth/login",
            new
            {
                identifier = "user-that-does-not-exist",
                secret = ValidSecret,
                previewReceipt = preview.PreviewReceipt
            });

        response.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();
    }

    [Fact]
    public async Task Login_WithUnknownPreviewReceipt_ShouldFallBackToNormalLoginValidation()
    {
        using var client = CreateClient(
            "receipt-forged-device-111111111111");

        var response = await client.PostAsJsonAsync(
            "/auth/login",
            new
            {
                identifier = ValidIdentifier,
                secret = ValidSecret,
                previewReceipt = "this-receipt-does-not-exist"
            });

        response.StatusCode.Should().Be(HttpStatusCode.Found);

        response.Headers
            .TryGetValues("Set-Cookie", out var cookies)
            .Should().BeTrue();

        cookies.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task PreviewReceipt_AfterSuccessfulCommit_ShouldBeConsumed()
    {
        using var client = CreateClient(
            "receipt-replay-device-111111111111");

        var previewResponse = await client.PostAsJsonAsync(
            "/auth/try-login",
            new
            {
                identifier = ValidIdentifier,
                secret = ValidSecret
            });

        var preview = await previewResponse.Content
            .ReadFromJsonAsync<TryLoginResult>();

        preview.Should().NotBeNull();
        preview!.PreviewReceipt.Should().NotBeNullOrWhiteSpace();

        var firstCommit = await client.PostAsJsonAsync(
            "/auth/login",
            new
            {
                identifier = ValidIdentifier,
                secret = ValidSecret,
                previewReceipt = preview.PreviewReceipt
            });

        firstCommit.StatusCode.Should().Be(HttpStatusCode.Found);

        firstCommit.Headers
            .TryGetValues("Set-Cookie", out var firstCookies)
            .Should().BeTrue();

        firstCookies.Should().NotBeNullOrEmpty();

        //
        // The receipt has now been consumed.
        //
        // Supplying it again must not make it an authentication
        // credential capable of bypassing password validation.
        //

        using var replayClient = CreateClient(
            "receipt-replay-device-111111111111");

        var replayResponse = await replayClient.PostAsJsonAsync(
            "/auth/login",
            new
            {
                identifier = ValidIdentifier,
                secret = "wrong-password",
                previewReceipt = preview.PreviewReceipt
            });

        replayResponse.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();
    }

    [Fact]
    public async Task Login_WithRepeatedInvalidCredentials_ShouldLockAccount()
    {
        using var client = CreateClient(
            "lockout-device-111111111111111111");

        var first = await client.PostAsJsonAsync(
            "/auth/login",
            new
            {
                identifier = ValidIdentifier,
                secret = "wrong-password-1"
            });

        first.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();

        var second = await client.PostAsJsonAsync(
            "/auth/login",
            new
            {
                identifier = ValidIdentifier,
                secret = "wrong-password-2"
            });

        second.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();

        // Correct credentials must not bypass an active lockout.
        var correctLogin = await client.PostAsJsonAsync(
            "/auth/login",
            new
            {
                identifier = ValidIdentifier,
                secret = ValidSecret
            });

        correctLogin.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();
    }

    [Fact]
    public async Task TryLogin_WithRepeatedInvalidCredentials_ShouldParticipateInLockout()
    {
        using var client = CreateClient(
            "try-lockout-device-222222222222222");

        var first = await client.PostAsJsonAsync(
            "/auth/try-login",
            new
            {
                identifier = ValidIdentifier,
                secret = "wrong-password-1"
            });

        var firstResult =
            await first.Content.ReadFromJsonAsync<TryLoginResult>();

        firstResult.Should().NotBeNull();
        firstResult!.Success.Should().BeFalse();

        var second = await client.PostAsJsonAsync(
            "/auth/try-login",
            new
            {
                identifier = ValidIdentifier,
                secret = "wrong-password-2"
            });

        var secondResult =
            await second.Content.ReadFromJsonAsync<TryLoginResult>();

        secondResult.Should().NotBeNull();
        secondResult!.Success.Should().BeFalse();

        // Account should now be locked.
        var correctAttempt = await client.PostAsJsonAsync(
            "/auth/try-login",
            new
            {
                identifier = ValidIdentifier,
                secret = ValidSecret
            });

        var correctResult =
            await correctAttempt.Content.ReadFromJsonAsync<TryLoginResult>();

        correctResult.Should().NotBeNull();
        correctResult!.Success.Should().BeFalse();
        correctResult.Reason.Should().Be(AuthFailureReason.LockedOut);
    }

    [Fact]
    public async Task TryLogin_WithValidCredentials_ShouldNotConsumeFailureAttempt()
    {
        using var client = CreateClient(
            "preview-no-failure-device-333333333333");

        var preview = await client.PostAsJsonAsync(
            "/auth/try-login",
            new
            {
                identifier = ValidIdentifier,
                secret = ValidSecret
            });

        var previewResult =
            await preview.Content.ReadFromJsonAsync<TryLoginResult>();

        previewResult.Should().NotBeNull();
        previewResult!.Success.Should().BeTrue();

        // First real failure.
        await client.PostAsJsonAsync(
            "/auth/login",
            new
            {
                identifier = ValidIdentifier,
                secret = "wrong-password"
            });

        // If successful TryLogin incorrectly consumed an attempt,
        // MaxFailedAttempts = 2 would have locked the account here.
        var correctLogin = await client.PostAsJsonAsync(
            "/auth/login",
            new
            {
                identifier = ValidIdentifier,
                secret = ValidSecret
            });

        correctLogin.Headers
            .TryGetValues("Set-Cookie", out var cookies)
            .Should().BeTrue();

        cookies.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task PreviewReceipt_WithDifferentSecret_ShouldNotSuppressFailureAccounting()
    {
        var user = await _factory.CreateLoginUserAsync();

        using var client = CreateClient(
            $"receipt-accounting-{Guid.NewGuid():N}");

        var previewResponse = await client.PostAsJsonAsync(
            "/auth/try-login",
            new
            {
                identifier = user.Identifier,
                secret = user.Secret
            });

        previewResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var preview =
            await previewResponse.Content.ReadFromJsonAsync<TryLoginResult>();

        preview.Should().NotBeNull();
        preview!.Success.Should().BeTrue();
        preview.PreviewReceipt.Should().NotBeNullOrWhiteSpace();

        // Receipt was created for the correct secret.
        // Using it with another secret must NOT suppress failure accounting.
        var firstFailure = await client.PostAsJsonAsync(
            "/auth/login",
            new
            {
                identifier = user.Identifier,
                secret = "wrong-password-1",
                previewReceipt = preview.PreviewReceipt
            });

        firstFailure.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();

        // MaxFailedAttempts = 2.
        // This must therefore be failure #2.
        var secondFailure = await client.PostAsJsonAsync(
            "/auth/login",
            new
            {
                identifier = user.Identifier,
                secret = "wrong-password-2"
            });

        secondFailure.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();

        // Account must now be locked. Correct credentials cannot authenticate.
        var correctLogin = await client.PostAsJsonAsync(
            "/auth/login",
            new
            {
                identifier = user.Identifier,
                secret = user.Secret
            });

        correctLogin.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();
    }

    [Fact]
    public async Task PreviewReceipt_FromDifferentDevice_ShouldNotSuppressFailureAccounting()
    {
        using var ownerClient = CreateClient(
            "receipt-owner-device-555555555555555");

        var previewResponse = await ownerClient.PostAsJsonAsync(
            "/auth/try-login",
            new
            {
                identifier = ValidIdentifier,
                secret = ValidSecret
            });

        var preview =
            await previewResponse.Content.ReadFromJsonAsync<TryLoginResult>();

        preview.Should().NotBeNull();
        preview!.Success.Should().BeTrue();
        preview.PreviewReceipt.Should().NotBeNullOrWhiteSpace();

        using var attackerClient = CreateClient(
            "receipt-attacker-device-666666666666");

        await attackerClient.PostAsJsonAsync(
            "/auth/login",
            new
            {
                identifier = ValidIdentifier,
                secret = "wrong-password-1",
                previewReceipt = preview.PreviewReceipt
            });

        await attackerClient.PostAsJsonAsync(
            "/auth/login",
            new
            {
                identifier = ValidIdentifier,
                secret = "wrong-password-2"
            });

        var correctLogin = await attackerClient.PostAsJsonAsync(
            "/auth/login",
            new
            {
                identifier = ValidIdentifier,
                secret = ValidSecret
            });

        correctLogin.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();
    }

    [Fact]
    public async Task SuccessfulLogin_ShouldResetPreviousFailureAccounting()
    {
        using var client = CreateClient(
            "failure-reset-device-777777777777777");

        // Failure #1
        await client.PostAsJsonAsync(
            "/auth/login",
            new
            {
                identifier = ValidIdentifier,
                secret = "wrong-password"
            });

        // Successful login should reset consecutive failure state.
        var success = await client.PostAsJsonAsync(
            "/auth/login",
            new
            {
                identifier = ValidIdentifier,
                secret = ValidSecret
            });

        success.Headers
            .TryGetValues("Set-Cookie", out var cookies)
            .Should().BeTrue();

        // Start another failure sequence.
        using var secondClient = CreateClient(
            "failure-reset-device-888888888888888");

        var failureAfterSuccess = await secondClient.PostAsJsonAsync(
            "/auth/login",
            new
            {
                identifier = ValidIdentifier,
                secret = "wrong-password-again"
            });

        failureAfterSuccess.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();

        // If the original failure wasn't reset, account would now
        // already be locked because MaxFailedAttempts = 2.
        var loginAgain = await secondClient.PostAsJsonAsync(
            "/auth/login",
            new
            {
                identifier = ValidIdentifier,
                secret = ValidSecret
            });

        loginAgain.Headers
            .TryGetValues("Set-Cookie", out var newCookies)
            .Should().BeTrue();

        newCookies.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Lockout_ShouldApplyAcrossDevices()
    {
        using var attackerDevice = CreateClient(
            "lockout-source-device-99999999999999");

        await attackerDevice.PostAsJsonAsync(
            "/auth/login",
            new
            {
                identifier = ValidIdentifier,
                secret = "wrong-password-1"
            });

        await attackerDevice.PostAsJsonAsync(
            "/auth/login",
            new
            {
                identifier = ValidIdentifier,
                secret = "wrong-password-2"
            });

        using var differentDevice = CreateClient(
            "lockout-other-device-000000000000000");

        var response = await differentDevice.PostAsJsonAsync(
            "/auth/login",
            new
            {
                identifier = ValidIdentifier,
                secret = ValidSecret
            });

        response.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();
    }

    #region HTTP Contract & Input Boundary

    [Fact]
    public async Task Login_WithFormPayload_ShouldAuthenticateUser()
    {
        using var client = CreateClient(
            "form-login-device-111111111111111");

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Identifier"] = ValidIdentifier,
            ["Secret"] = ValidSecret
        });

        var response = await client.PostAsync("/auth/login", content);

        response.StatusCode.Should().Be(HttpStatusCode.Found);

        response.Headers
            .TryGetValues("Set-Cookie", out var cookies)
            .Should().BeTrue();

        cookies.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task TryLogin_WithFormPayload_ShouldReturnSuccessfulPreview()
    {
        using var client = CreateClient(
            "form-preview-device-222222222222222");

        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Identifier"] = ValidIdentifier,
            ["Secret"] = ValidSecret
        });

        var response = await client.PostAsync("/auth/try-login", content);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<TryLoginResult>();

        result.Should().NotBeNull();
        result!.Success.Should().BeTrue();
        result.PreviewReceipt.Should().NotBeNullOrWhiteSpace();

        response.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();
    }

    [Theory]
    [InlineData("", "admin")]
    [InlineData(" ", "admin")]
    [InlineData("admin", "")]
    [InlineData("admin", " ")]
    [InlineData("", "")]
    [InlineData(" ", " ")]
    public async Task Login_WithMissingOrWhitespaceCredentials_ShouldNotAuthenticate(
        string identifier,
        string secret)
    {
        using var client = CreateClient(
            $"invalid-input-device-{Guid.NewGuid():N}");

        var response = await client.PostAsJsonAsync(
            "/auth/login",
            new
            {
                identifier,
                secret
            });

        response.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();
    }

    [Theory]
    [InlineData("", "admin")]
    [InlineData(" ", "admin")]
    [InlineData("admin", "")]
    [InlineData("admin", " ")]
    [InlineData("", "")]
    [InlineData(" ", " ")]
    public async Task TryLogin_WithMissingOrWhitespaceCredentials_ShouldReturnInvalidCredentials(
        string identifier,
        string secret)
    {
        using var client = CreateClient(
            $"invalid-preview-device-{Guid.NewGuid():N}");

        var response = await client.PostAsJsonAsync(
            "/auth/try-login",
            new
            {
                identifier,
                secret
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<TryLoginResult>();

        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Reason.Should().Be(AuthFailureReason.InvalidCredentials);
        result.PreviewReceipt.Should().BeNull();
    }

    [Fact]
    public async Task TryLogin_WithUnsupportedContentType_ShouldReturnBadRequest()
    {
        using var client = CreateClient(
            "content-type-device-333333333333333");

        using var content = new StringContent(
            "identifier=admin&secret=admin",
            System.Text.Encoding.UTF8,
            "text/plain");

        var response = await client.PostAsync("/auth/try-login", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        response.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();
    }

    [Fact]
    public async Task Login_WithUnsupportedContentType_ShouldNotAuthenticate()
    {
        using var client = CreateClient(
            "login-content-type-device-444444444");

        using var content = new StringContent(
            "identifier=admin&secret=admin",
            System.Text.Encoding.UTF8,
            "text/plain");

        var response = await client.PostAsync("/auth/login", content);

        response.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();

        ((int)response.StatusCode)
            .Should().BeLessThan(500);
    }

    [Fact]
    public async Task TryLogin_WithEmptyJsonObject_ShouldReturnInvalidCredentials()
    {
        using var client = CreateClient(
            "empty-json-device-555555555555555");

        var response = await client.PostAsJsonAsync(
            "/auth/try-login",
            new { });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<TryLoginResult>();

        result.Should().NotBeNull();
        result!.Success.Should().BeFalse();
        result.Reason.Should().Be(AuthFailureReason.InvalidCredentials);
        result.PreviewReceipt.Should().BeNull();
    }

    [Fact]
    public async Task Login_WithEmptyJsonObject_ShouldNotAuthenticate()
    {
        using var client = CreateClient(
            "empty-login-json-device-66666666666");

        var response = await client.PostAsJsonAsync(
            "/auth/login",
            new { });

        response.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();

        ((int)response.StatusCode)
            .Should().BeLessThan(500);
    }

    [Fact]
    public async Task Login_WithJsonPropertyNamesUsingDifferentCasing_ShouldAuthenticate()
    {
        var user = await _factory.CreateLoginUserAsync();

        using var client = CreateClient(
            $"json-casing-{Guid.NewGuid():N}");

        using var content = JsonContent.Create(new Dictionary<string, string>
        {
            ["IDENTIFIER"] = user.Identifier,
            ["SECRET"] = user.Secret
        });

        var response = await client.PostAsync(
            LoginEndpoint,
            content);

        response.StatusCode.Should().Be(HttpStatusCode.Found);

        response.Headers
            .TryGetValues("Set-Cookie", out var cookies)
            .Should().BeTrue();

        cookies.Should().NotBeNullOrEmpty();

        GetSessionCookie(response)
            .Should().NotBeNullOrWhiteSpace();
    }

    #endregion

    #region Identifier Enumeration & Failure Disclosure

    [Fact]
    public async Task Login_WithUnknownIdentifier_ShouldNotRevealWhetherUserExists()
    {
        using var unknownUserClient = CreateClient(
            $"enumeration-unknown-{Guid.NewGuid():N}");

        using var existingUserClient = CreateClient(
            $"enumeration-existing-{Guid.NewGuid():N}");

        var unknownResponse = await unknownUserClient.PostAsJsonAsync(
            "/auth/login",
            new
            {
                identifier = $"unknown-{Guid.NewGuid():N}",
                secret = "wrong-password"
            });

        var existingResponse = await existingUserClient.PostAsJsonAsync(
            "/auth/login",
            new
            {
                identifier = ValidIdentifier,
                secret = "wrong-password"
            });

        unknownResponse.StatusCode.Should().Be(existingResponse.StatusCode);

        unknownResponse.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();

        existingResponse.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();
    }

    [Fact]
    public async Task TryLogin_WithUnknownIdentifier_ShouldReturnSameFailureReasonAsWrongPassword()
    {
        var existingUser = await _factory.CreateLoginUserAsync();

        using var unknownClient = CreateClient(
            $"unknown-{Guid.NewGuid():N}");

        using var existingClient = CreateClient(
            $"existing-{Guid.NewGuid():N}");

        var unknownResponse = await unknownClient.PostAsJsonAsync(
            "/auth/try-login",
            new
            {
                identifier = $"unknown-{Guid.NewGuid():N}",
                secret = "wrong-password"
            });

        var existingResponse = await existingClient.PostAsJsonAsync(
            "/auth/try-login",
            new
            {
                identifier = existingUser.Identifier,
                secret = "wrong-password"
            });

        unknownResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        existingResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var unknownResult =
            await unknownResponse.Content.ReadFromJsonAsync<TryLoginResult>();

        var existingResult =
            await existingResponse.Content.ReadFromJsonAsync<TryLoginResult>();

        unknownResult.Should().NotBeNull();
        existingResult.Should().NotBeNull();

        unknownResult!.Success.Should().BeFalse();
        existingResult!.Success.Should().BeFalse();

        unknownResult.Reason
            .Should().Be(AuthFailureReason.InvalidCredentials);

        existingResult.Reason
            .Should().Be(AuthFailureReason.InvalidCredentials);

        unknownResult.PreviewReceipt.Should().BeNullOrWhiteSpace();
        existingResult.PreviewReceipt.Should().BeNullOrWhiteSpace();
    }

    [Fact]
    public async Task TryLogin_WithUnknownIdentifier_ShouldNotIssuePreviewReceipt()
    {
        using var client = CreateClient(
            $"unknown-receipt-{Guid.NewGuid():N}");

        var response = await client.PostAsJsonAsync(
            "/auth/try-login",
            new
            {
                identifier = $"unknown-{Guid.NewGuid():N}",
                secret = "some-password"
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result =
            await response.Content.ReadFromJsonAsync<TryLoginResult>();

        result.Should().NotBeNull();

        result!.Success.Should().BeFalse();
        result.Reason.Should().Be(AuthFailureReason.InvalidCredentials);
        result.PreviewReceipt.Should().BeNullOrWhiteSpace();

        response.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();
    }

    [Fact]
    public async Task TryLogin_WithWrongPassword_ShouldNotIssuePreviewReceipt()
    {
        var user = await _factory.CreateLoginUserAsync();

        using var client = CreateClient(
            $"wrong-password-{Guid.NewGuid():N}");

        var response = await client.PostAsJsonAsync(
            "/auth/try-login",
            new
            {
                identifier = user.Identifier,
                secret = "definitely-wrong-password"
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result =
            await response.Content.ReadFromJsonAsync<TryLoginResult>();

        result.Should().NotBeNull();

        result!.Success.Should().BeFalse();
        result.Reason.Should().Be(AuthFailureReason.InvalidCredentials);
        result.PreviewReceipt.Should().BeNullOrWhiteSpace();

        response.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();
    }

    [Fact]
    public async Task Login_WithUnknownIdentifier_ShouldNotReturnAuthenticationCredential()
    {
        using var client = CreateClient(
            $"unknown-credential-{Guid.NewGuid():N}");

        var response = await client.PostAsJsonAsync(
            "/auth/login",
            new
            {
                identifier = $"unknown-{Guid.NewGuid():N}",
                secret = ValidSecret
            });

        response.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();

        ((int)response.StatusCode)
            .Should().BeLessThan(500);
    }

    #endregion

    [Fact]
    public async Task Login_FailuresOutsideFailureWindow_ShouldNotAccumulateTowardLockout()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();

        using var client = CreateClient(
            $"failure-window-{Guid.NewGuid():N}");

        // Failure #1
        var firstFailure = await LoginAsync(
            client,
            user.Identifier,
            "wrong-password-1");

        firstFailure.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();

        // Default FailureWindow = 15 minutes.
        // Move beyond the window.
        _factory.Clock.Advance(
            TimeSpan.FromMinutes(16));

        // This should begin a new failure window,
        // rather than becoming failure #2 of the old window.
        var secondFailure = await LoginAsync(
            client,
            user.Identifier,
            "wrong-password-2");

        secondFailure.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();

        // If the old failure was incorrectly retained,
        // MaxFailedAttempts = 2 would have locked the account.
        var correctLogin = await LoginAsync(
            client,
            user.Identifier,
            user.Secret);

        correctLogin.Headers
            .TryGetValues("Set-Cookie", out var cookies)
            .Should().BeTrue();

        cookies.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_BeforeLockoutExpires_ShouldRemainLocked()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();

        using var client = CreateClient(
            $"active-lockout-{Guid.NewGuid():N}");

        await LoginAsync(
            client,
            user.Identifier,
            "wrong-password-1");

        await LoginAsync(
            client,
            user.Identifier,
            "wrong-password-2");

        _factory.Clock.Advance(
            TimeSpan.FromSeconds(9));

        var response = await LoginAsync(
            client,
            user.Identifier,
            user.Secret);

        response.Headers
            .TryGetValues("Set-Cookie", out _)
            .Should().BeFalse();
    }

    [Fact]
    public async Task Login_AtLockoutExpirationBoundary_ShouldAllowCorrectCredentials()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();

        using var client = CreateClient(
            $"lockout-boundary-{Guid.NewGuid():N}");

        await LoginAsync(
            client,
            user.Identifier,
            "wrong-password-1");

        await LoginAsync(
            client,
            user.Identifier,
            "wrong-password-2");

        _factory.Clock.Advance(
            TimeSpan.FromSeconds(10));

        var response = await LoginAsync(
            client,
            user.Identifier,
            user.Secret);

        response.Headers
            .TryGetValues("Set-Cookie", out var cookies)
            .Should().BeTrue();

        cookies.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Login_FailuresWithinFailureWindow_ShouldAccumulateTowardLockout()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();

        using var client = CreateClient(
            $"failure-window-inside-{Guid.NewGuid():N}");

        var first = await TryLoginAsync(
            client,
            user.Identifier,
            "wrong-password-1");

        first.Success.Should().BeFalse();
        first.Reason.Should().Be(AuthFailureReason.InvalidCredentials);
        first.RemainingAttempts.Should().Be(1);
        first.LockoutUntilUtc.Should().BeNull();

        _factory.Clock.Advance(TimeSpan.FromMinutes(14));

        var second = await TryLoginAsync(
            client,
            user.Identifier,
            "wrong-password-2");

        second.Success.Should().BeFalse();
        second.Reason.Should().Be(AuthFailureReason.LockedOut);
        second.RemainingAttempts.Should().Be(0);
        second.LockoutUntilUtc.Should().NotBeNull();
    }

    [Fact]
    public async Task Login_FailureDuringLockout_ShouldNotExtendLockout_WhenExtendLockOnFailureIsDisabled()
    {
        _factory.Clock.Reset();

        var user = await _factory.CreateLoginUserAsync();

        using var client = CreateClient(
            $"lockout-no-extension-{Guid.NewGuid():N}");

        var first = await TryLoginAsync(
            client,
            user.Identifier,
            "wrong-password-1");

        first.Success.Should().BeFalse();
        first.Reason.Should().Be(AuthFailureReason.InvalidCredentials);

        var second = await TryLoginAsync(
            client,
            user.Identifier,
            "wrong-password-2");

        second.Success.Should().BeFalse();
        second.Reason.Should().Be(AuthFailureReason.LockedOut);
        second.LockoutUntilUtc.Should().NotBeNull();

        var originalLockoutUntil = second.LockoutUntilUtc!.Value;

        _factory.Clock.Advance(TimeSpan.FromSeconds(5));

        var duringLockout = await TryLoginAsync(
            client,
            user.Identifier,
            "still-wrong");

        duringLockout.Success.Should().BeFalse();
        duringLockout.Reason.Should().Be(AuthFailureReason.LockedOut);

        duringLockout.LockoutUntilUtc
            .Should().Be(originalLockoutUntil);

        _factory.Clock.Set(
            originalLockoutUntil.AddMilliseconds(1));

        var login = await LoginAsync(
            client,
            user.Identifier,
            user.Secret);

        login.StatusCode.Should().Be(HttpStatusCode.Found);

        login.Headers
            .TryGetValues("Set-Cookie", out var cookies)
            .Should().BeTrue();

        cookies.Should().NotBeNullOrEmpty();
    }

    //[Fact]
    //public async Task ConcurrentLoginFailures_ShouldNotLoseFailureAttempts()
    //{
    //    _factory.Clock.Reset();

    //    var user = await _factory.CreateLoginUserAsync();

    //    using var client1 = CreateClient(
    //        $"concurrent-failure-1-{Guid.NewGuid():N}");

    //    using var client2 = CreateClient(
    //        $"concurrent-failure-2-{Guid.NewGuid():N}");

    //    var task1 = TryLoginAsync(
    //        client1,
    //        user.Identifier,
    //        "wrong-password-1");

    //    var task2 = TryLoginAsync(
    //        client2,
    //        user.Identifier,
    //        "wrong-password-2");

    //    var results = await Task.WhenAll(task1, task2);

    //    results.Should().OnlyContain(x => !x.Success);

    //    using var verificationClient = CreateClient(
    //        $"concurrent-failure-verification-{Guid.NewGuid():N}");

    //    var verification = await TryLoginAsync(
    //        verificationClient,
    //        user.Identifier,
    //        user.Secret);

    //    verification.Success.Should().BeFalse();
    //    verification.Reason.Should().Be(AuthFailureReason.LockedOut);
    //    verification.RemainingAttempts.Should().Be(0);
    //    verification.LockoutUntilUtc.Should().NotBeNull();
    //}

    //[Fact]
    //public async Task ConcurrentSuccessfulLogins_FromDifferentDevices_ShouldCreateUsableSessions()
    //{
    //    _factory.Clock.Reset();

    //    var user = await _factory.CreateLoginUserAsync();

    //    var device1 = $"concurrent-success-1-{Guid.NewGuid():N}";
    //    var device2 = $"concurrent-success-2-{Guid.NewGuid():N}";

    //    using var client1 = CreateClient(device1);
    //    using var client2 = CreateClient(device2);

    //    var responses = await Task.WhenAll(
    //        LoginAsync(client1, user.Identifier, user.Secret),
    //        LoginAsync(client2, user.Identifier, user.Secret));

    //    responses.Should().OnlyContain(
    //        x => x.StatusCode == HttpStatusCode.Found);

    //    var cookie1 = responses[0]
    //        .Headers
    //        .GetValues("Set-Cookie")
    //        .First();

    //    var cookie2 = responses[1]
    //        .Headers
    //        .GetValues("Set-Cookie")
    //        .First();

    //    cookie1.Should().NotBeNullOrWhiteSpace();
    //    cookie2.Should().NotBeNullOrWhiteSpace();
    //    cookie1.Should().NotBe(cookie2);

    //    using var authenticatedClient1 = CreateClient(device1);
    //    using var authenticatedClient2 = CreateClient(device2);

    //    authenticatedClient1.DefaultRequestHeaders.Add(
    //        "Cookie",
    //        cookie1);

    //    authenticatedClient2.DefaultRequestHeaders.Add(
    //        "Cookie",
    //        cookie2);

    //    var meResponses = await Task.WhenAll(
    //        authenticatedClient1.PostAsJsonAsync(
    //            "/auth/me/profile/get",
    //            new GetProfileRequest { ProfileKey = null }),

    //        authenticatedClient2.PostAsJsonAsync(
    //            "/auth/me/profile/get",
    //            new GetProfileRequest { ProfileKey = null }));

    //    meResponses.Should().OnlyContain(
    //        x => x.StatusCode == HttpStatusCode.OK);
    //}

    //[Fact]
    //public async Task ConcurrentLogin_WithSamePreviewReceipt_ShouldNotAllowReceiptToBeConsumedTwice()
    //{
    //    _factory.Clock.Reset();

    //    var user = await _factory.CreateLoginUserAsync();

    //    var device = $"preview-concurrent-{Guid.NewGuid():N}";

    //    using var previewClient = CreateClient(device);

    //    var preview = await TryLoginAsync(
    //        previewClient,
    //        user.Identifier,
    //        user.Secret);

    //    preview.Success.Should().BeTrue();
    //    preview.PreviewReceipt.Should().NotBeNullOrWhiteSpace();

    //    var receipt = preview.PreviewReceipt!;

    //    using var client1 = CreateClient(device);
    //    using var client2 = CreateClient(device);

    //    var responses = await Task.WhenAll(
    //        LoginWithPreviewReceiptAsync(
    //            client1,
    //            user.Identifier,
    //            user.Secret,
    //            receipt),

    //        LoginWithPreviewReceiptAsync(
    //            client2,
    //            user.Identifier,
    //            user.Secret,
    //            receipt));

    //    responses.Should().OnlyContain(
    //        x => x.StatusCode == HttpStatusCode.Found);

    //    using var replayClient = CreateClient(device);

    //    var replay = await LoginWithPreviewReceiptAsync(
    //        replayClient,
    //        user.Identifier,
    //        "wrong-password",
    //        receipt);

    //    replay.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

    //    var failureState = await TryLoginAsync(
    //        replayClient,
    //        user.Identifier,
    //        "another-wrong-password");

    //    failureState.Success.Should().BeFalse();
    //    failureState.Reason.Should().Be(AuthFailureReason.LockedOut);
    //}


    private HttpClient CreateClient(
        string deviceId = "test-device-1234567890123456")
    {
        var client = _factory.CreateClient(
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

    private static Task<HttpResponseMessage> LoginAsync(HttpClient client, string identifier, string secret)
    {
        return client.PostAsJsonAsync(
            LoginEndpoint,
            new
            {
                identifier,
                secret
            });
    }

    private static async Task<TryLoginResult> TryLoginAsync(HttpClient client, string identifier, string secret)
    {
        var response = await client.PostAsJsonAsync(
            "auth/try-login",
            new LoginRequest
            {
                Identifier = identifier,
                Secret = secret
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<TryLoginResult>();

        result.Should().NotBeNull();

        return result!;
    }

    private static Task<HttpResponseMessage> LoginWithPreviewReceiptAsync(HttpClient client, string identifier, string secret, string previewReceipt)
    {
        return client.PostAsJsonAsync("/auth/login", new
        {
            identifier,
            secret,
            previewReceipt
        });
    }

    private static string GetSessionCookie(
        HttpResponseMessage response)
    {
        response.Headers
            .TryGetValues("Set-Cookie", out var cookies)
            .Should().BeTrue();

        cookies.Should().NotBeNullOrEmpty();

        return cookies!.First();
    }

    private void AdvanceClock(TimeSpan duration)
    {
        _factory.Clock.Advance(duration);
    }

    private DateTimeOffset GetUtcNow()
    {
        return _factory.Clock.UtcNow;
    }
}
