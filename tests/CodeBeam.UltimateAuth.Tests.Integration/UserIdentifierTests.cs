using CodeBeam.UltimateAuth.Tests.Integration.Infrastructure;
using CodeBeam.UltimateAuth.Users.Contracts;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

namespace CodeBeam.UltimateAuth.Tests.Integration;

public sealed class UserIdentifierTests : IClassFixture<AuthServerFactory>
{
    private readonly AuthServerFactory _factory;

    public UserIdentifierTests(AuthServerFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Availability_WhenUsernameDoesNotExist_ShouldReturnAvailable()
    {
        _factory.Clock.Reset();

        using var client = CreateClient();

        var username = $"available-{Guid.NewGuid():N}";

        var response = await client.PostAsJsonAsync("/auth/me/identifiers/check-availability",
            new CheckUserIdentifierAvailabilityRequest
            {
                Type = UserIdentifierType.Username,
                Value = username
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<UserIdentifierAvailabilityResult>();

        result.Should().NotBeNull();

        result!.IsValid.Should().BeTrue();
        result.IsAvailable.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.NormalizedValue.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Availability_WhenUsernameAlreadyExists_ShouldReturnUnavailable()
    {
        _factory.Clock.Reset();

        using var client = CreateClient();

        var username = $"taken-{Guid.NewGuid():N}";

        var create = await client.PostAsJsonAsync("/auth/users/create",
            new CreateUserRequest
            {
                UserName = username,
                Password = $"Test-{Guid.NewGuid():N}!",
                DisplayName = username
            });

        create.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await client.PostAsJsonAsync(
            "/auth/me/identifiers/check-availability",
            new CheckUserIdentifierAvailabilityRequest
            {
                Type = UserIdentifierType.Username,
                Value = username
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<UserIdentifierAvailabilityResult>();

        result.Should().NotBeNull();

        result!.IsValid.Should().BeTrue();
        result.IsAvailable.Should().BeFalse();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public async Task Availability_ShouldUseNormalizedIdentifier()
    {
        _factory.Clock.Reset();

        using var client = CreateClient();

        var unique = Guid.NewGuid().ToString("N");

        var email = $"availability-{unique}@example.com";

        var create = await client.PostAsJsonAsync("/auth/users/create",
            new CreateUserRequest
            {
                UserName = $"user-{unique}",
                Email = email,
                Password = $"Test-{Guid.NewGuid():N}!",
                DisplayName = "Availability Test"
            });

        create.StatusCode.Should().Be(HttpStatusCode.OK);

        // Same logical identifier, different representation.
        var response = await client.PostAsJsonAsync(
            "/auth/me/identifiers/check-availability",
            new CheckUserIdentifierAvailabilityRequest
            {
                Type = UserIdentifierType.Email,
                Value = $"  {email.ToUpperInvariant()}  "
            });

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<UserIdentifierAvailabilityResult>();

        result.Should().NotBeNull();

        result!.IsValid.Should().BeTrue();
        result.IsAvailable.Should().BeFalse();
        result.Errors.Should().BeEmpty();

        result.NormalizedValue.Should().Be(email.ToLowerInvariant());
    }

    [Fact]
    public async Task Availability_WhenIdentifierIsInvalid_ShouldReturnValidationResult()
    {
        _factory.Clock.Reset();

        using var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            "/auth/me/identifiers/check-availability",
            new CheckUserIdentifierAvailabilityRequest
            {
                Type = UserIdentifierType.Email,
                Value = "not-an-email"
            });

        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var result =
            await response.Content
                .ReadFromJsonAsync<UserIdentifierAvailabilityResult>();

        result.Should().NotBeNull();

        result!.IsValid.Should().BeFalse();
        result.IsAvailable.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Availability_WhenIdentifierIsEmpty_ShouldReturnValidationResult()
    {
        _factory.Clock.Reset();

        using var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            "/auth/me/identifiers/check-availability",
            new CheckUserIdentifierAvailabilityRequest
            {
                Type = UserIdentifierType.Username,
                Value = "   "
            });

        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var result =
            await response.Content
                .ReadFromJsonAsync<UserIdentifierAvailabilityResult>();

        result.Should().NotBeNull();

        result!.IsValid.Should().BeFalse();
        result.IsAvailable.Should().BeFalse();
        result.Errors.Should().NotBeEmpty();
    }

    // TODO:
    // Expand UserIdentifier integration coverage:
    //
    // - Add identifier (self/admin)
    // - Update identifier (self/admin)
    // - Delete identifier (self/admin)
    // - Set/unset primary
    // - Verify identifier
    // - Duplicate identifier policies
    // - Username/email/phone uniqueness policies
    // - Tenant isolation
    // - Soft-deleted identifier availability semantics
    // - Authorization / endpoint permission checks
    // - Optimistic concurrency
    // - Multi-profile interactions if identifier ownership evolves
    // - Client SDK end-to-end coverage

    private HttpClient CreateClient()
    {
        var client = _factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = false
            });

        client.DefaultRequestHeaders.Add("Origin", "https://localhost:6130");
        client.DefaultRequestHeaders.Add("X-UDID", $"user-identifier-{Guid.NewGuid():N}");

        return client;
    }
}