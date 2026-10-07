using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Infrastructure;
using CodeBeam.UltimateAuth.Tests.Integration.Infrastructure;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
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

    [Theory]
    [InlineData("update", false)]
    [InlineData("set-primary", false)]
    [InlineData("unset-primary", false)]
    [InlineData("verify", false)]
    [InlineData("delete", false)]
    [InlineData("update", true)]
    [InlineData("set-primary", true)]
    [InlineData("unset-primary", true)]
    [InlineData("verify", true)]
    [InlineData("delete", true)]
    public async Task IdentifierMutation_WhenIdBelongsToAnotherUser_ShouldReturnNotFoundAndPreserveRecords(
        string operation, bool admin)
    {
        var actor = await _factory.CreateLoginUserAsync();
        var victim = await _factory.CreateLoginUserAsync();
        if (admin)
            await _factory.GrantPermissionsAsync(actor.UserKey, new[] { "*" });

        using var scope = _factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var store = services.GetRequiredService<IUserIdentifierStoreFactory>()
            .Create(TenantKeys.Single);
        var normalizer = services.GetRequiredService<IIdentifierNormalizer>();
        var value = $"ownership-{Guid.NewGuid():N}@example.com";
        var identifier = UserIdentifier.Create(
            Guid.NewGuid(), TenantKeys.Single, victim.UserKey,
            UserIdentifierType.Email, value,
            normalizer.Normalize(UserIdentifierType.Email, value).Normalized,
            _factory.Clock.UtcNow,
            isPrimary: operation == "unset-primary",
            verifiedAt: operation == "verify" ? null : _factory.Clock.UtcNow);
        await store.AddAsync(identifier);

        var before = (await store.GetByUserAsync(victim.UserKey))
            .Select(x => x.Snapshot()).ToArray();
        using var client = CreateClient();
        await AuthenticateIdentifierClientAsync(client, actor.Identifier, actor.Secret);

        // Prove authentication works before exercising the ownership boundary.
        using var authenticated = await client.PostAsJsonAsync(
            "/auth/me/identifiers/get", new { });
        authenticated.StatusCode.Should().Be(HttpStatusCode.OK);

        var path = admin
            ? $"/auth/admin/users/{actor.UserKey.Value}/identifiers/{operation}"
            : $"/auth/me/identifiers/{operation}";

        // Admin policies forbid modifying the actor themselves.
        // Use a separate route target whose identifiers are NOT the victim's.
        if (admin)
        {
            var target = await _factory.CreateLoginUserAsync();
            path = $"/auth/admin/users/{target.UserKey.Value}/identifiers/{operation}";
        }

        using var response = await client.PostAsJsonAsync(path, new
        {
            id = identifier.Id,
            newValue = $"changed-{Guid.NewGuid():N}@example.com",
            mode = 0 // DeleteMode.Soft
        });

        var after = await store.GetByUserAsync(victim.UserKey);
        using var assertions = new FluentAssertions.Execution.AssertionScope();
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        after.Should().BeEquivalentTo(before,
            "rejected operations must not mutate the victim or their primary identifiers");
    }

    [Theory]
    [InlineData("update")]
    [InlineData("set-primary")]
    [InlineData("unset-primary")]
    [InlineData("verify")]
    [InlineData("delete")]
    public async Task IdentifierMutation_WhenIdBelongsToSelf_ShouldSucceed(string operation)
    {
        using var configuredFactory = operation == "verify"
            ? AuthServerFactory.CreateWithServices(services =>
                services.AddSingleton<IUserIdentifierVerifier, TestUserIdentifierVerifier>())
            : null;

        var factory = configuredFactory ?? _factory;

        var owner = await factory.CreateLoginUserAsync();
        using var scope = factory.Services.CreateScope();
        var services = scope.ServiceProvider;
        var store = services.GetRequiredService<IUserIdentifierStoreFactory>().Create(TenantKeys.Single);
        var normalizer = services.GetRequiredService<IIdentifierNormalizer>();
        var value = $"owned-{Guid.NewGuid():N}@example.com";
        var identifier = UserIdentifier.Create(
            Guid.NewGuid(), TenantKeys.Single, owner.UserKey,
            UserIdentifierType.Email, value,
            normalizer.Normalize(UserIdentifierType.Email, value).Normalized,
            factory.Clock.UtcNow,
            isPrimary: operation == "unset-primary",
            verifiedAt: operation == "verify" ? null : factory.Clock.UtcNow);
        await store.AddAsync(identifier);
        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = false
            });

        client.DefaultRequestHeaders.Add(
            "Origin", "https://localhost:6130");

        client.DefaultRequestHeaders.Add(
            "X-UDID", $"identifier-owner-{Guid.NewGuid():N}");
        await AuthenticateIdentifierClientAsync(client, owner.Identifier, owner.Secret);
        var newValue = $"updated-{Guid.NewGuid():N}@example.com";
        using var response = await client.PostAsJsonAsync(
            $"/auth/me/identifiers/{operation}",
            new
            {
                id = identifier.Id,
                newValue,
                mode = 0,
                proof = operation == "verify"
                    ? TestUserIdentifierVerifier.ValidProof
                    : null
            });
        response.IsSuccessStatusCode.Should().BeTrue(
            $"own identifier operation should succeed: {await response.Content.ReadAsStringAsync()}");
        var saved = await store.GetByIdAsync(identifier.Id);
        saved.Should().NotBeNull();
        switch (operation)
        {
            case "update": saved!.Value.Should().Be(newValue); break;
            case "set-primary": saved!.IsPrimary.Should().BeTrue(); break;
            case "unset-primary": saved!.IsPrimary.Should().BeFalse(); break;
            case "verify": saved!.IsVerified.Should().BeTrue(); break;
            case "delete": saved!.IsDeleted.Should().BeTrue(); break;
        }
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

    private static async Task AuthenticateIdentifierClientAsync(HttpClient client, string identifier, string secret)
    {
        using var login = await client.PostAsJsonAsync("/auth/login", new { identifier, secret });
        login.StatusCode.Should().Be(HttpStatusCode.Found);
        login.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();

        // Send only cookie name/value pairs, not Set-Cookie attributes.
        client.DefaultRequestHeaders.Add("Cookie", string.Join("; ", cookies!.Select(x => x.Split(';')[0])));
    }

    private sealed class TestUserIdentifierVerifier : IUserIdentifierVerifier
    {
        public const string ValidProof = "integration-valid-proof";

        public Task<bool> VerifyAsync(UserIdentifierVerificationContext context, CancellationToken ct = default)
        {
            ct.ThrowIfCancellationRequested();

            return Task.FromResult(context.Proof == ValidProof);
        }
    }
}
