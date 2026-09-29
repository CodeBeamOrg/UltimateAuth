using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Json;

namespace CodeBeam.UltimateAuth.Tests.Integration;

public sealed class UserLifecycleTests : IClassFixture<AuthServerFactory>
{
    private readonly AuthServerFactory _factory;

    public UserLifecycleTests(AuthServerFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateUser_ShouldCreateUsableUser()
    {
        _factory.Clock.Reset();

        using var client = CreateClient();

        var username = $"user-{Guid.NewGuid():N}";
        var secret = $"Test-{Guid.NewGuid():N}!";

        var response = await client.PostAsJsonAsync(
            "/auth/users/create",
            new CreateUserRequest
            {
                UserName = username,
                Password = secret,
                DisplayName = "Integration User"
            });

        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var result =
            await response.Content.ReadFromJsonAsync<UserCreateResult>();

        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();
        result.UserKey.Should().NotBe(default);

        //
        // The aggregate must be usable, not merely persisted.
        //
        var login = await client.PostAsJsonAsync(
            "/auth/login",
            new
            {
                identifier = username,
                secret
            });

        login.StatusCode.Should()
            .Be(HttpStatusCode.Found);
    }

    [Fact]
    public async Task CreateUser_ShouldCreateLifecycle()
    {
        _factory.Clock.Reset();

        using var client = CreateClient();

        var username = $"lifecycle-{Guid.NewGuid():N}";

        var result = await CreateUserAsync(
            client,
            username);

        var userKey = GetUserKey(result);

        using var scope =
            _factory.Services.CreateScope();

        var factory =
            scope.ServiceProvider
                .GetRequiredService<IUserLifecycleStoreFactory>();

        var store =
            factory.Create(TenantKeys.Single);

        var lifecycle = await store.GetAsync(
            new UserLifecycleKey(
                TenantKeys.Single,
                userKey));

        lifecycle.Should().NotBeNull();

        lifecycle!.UserKey.Should()
            .Be(userKey);

        lifecycle.Tenant.Should()
            .Be(TenantKeys.Single);

        lifecycle.IsDeleted.Should()
            .BeFalse();

        lifecycle.Status.Should()
            .Be(UserStatus.Active);

        lifecycle.CreatedAt.Should()
            .Be(_factory.Clock.UtcNow);
    }

    [Fact]
    public async Task CreateUser_ShouldCreateDefaultProfile()
    {
        _factory.Clock.Reset();

        using var client = CreateClient();

        var username = $"profile-{Guid.NewGuid():N}";
        var displayName = "Lifecycle Integration User";

        var result = await CreateUserAsync(
            client,
            username,
            displayName: displayName);

        var userKey = GetUserKey(result);

        using var scope =
            _factory.Services.CreateScope();

        var factory =
            scope.ServiceProvider
                .GetRequiredService<IUserProfileStoreFactory>();

        var store =
            factory.Create(TenantKeys.Single);

        var profile = await store.GetAsync(
            new UserProfileKey(
                TenantKeys.Single,
                userKey,
                ProfileKey.Default));

        profile.Should().NotBeNull();

        profile!.UserKey.Should()
            .Be(userKey);

        profile.ProfileKey.Should()
            .Be(ProfileKey.Default);

        profile.DisplayName.Should()
            .Be(displayName);

        profile.IsDeleted.Should()
            .BeFalse();
    }

    [Fact]
    public async Task CreateUser_WithUsername_ShouldCreatePrimaryUsername()
    {
        _factory.Clock.Reset();

        using var client = CreateClient();

        var username =
            $"identifier-{Guid.NewGuid():N}";

        var result = await CreateUserAsync(
            client,
            username);

        var userKey = GetUserKey(result);

        using var scope =
            _factory.Services.CreateScope();

        var factory =
            scope.ServiceProvider
                .GetRequiredService<IUserIdentifierStoreFactory>();

        var store =
            factory.Create(TenantKeys.Single);

        var identifiers =
            await store.GetByUserAsync(
                userKey);

        var identifier = identifiers
            .Single(x =>
                x.Type == UserIdentifierType.Username);

        identifier.Value.Should()
            .Be(username);

        identifier.UserKey.Should()
            .Be(userKey);

        identifier.IsPrimary.Should()
            .BeTrue();

        identifier.IsDeleted.Should()
            .BeFalse();
    }

    [Fact]
    public async Task CreateUser_WithVerifiedEmail_ShouldPersistEmailAsVerified()
    {
        _factory.Clock.Reset();

        using var client = CreateClient();

        var username =
            $"email-user-{Guid.NewGuid():N}";

        var email =
            $"{Guid.NewGuid():N}@example.com";

        var result = await CreateUserAsync(
            client,
            username,
            email: email,
            emailVerified: true);

        var userKey = GetUserKey(result);

        using var scope =
            _factory.Services.CreateScope();

        var factory =
            scope.ServiceProvider
                .GetRequiredService<IUserIdentifierStoreFactory>();

        var store =
            factory.Create(TenantKeys.Single);

        var identifiers =
            await store.GetByUserAsync(
                userKey);

        var identifier = identifiers
            .Single(x =>
                x.Type == UserIdentifierType.Email);

        identifier.Value.Should()
            .Be(email);

        identifier.IsPrimary.Should()
            .BeTrue();

        identifier.IsVerified.Should()
            .BeTrue();

        identifier.VerifiedAt.Should()
            .Be(_factory.Clock.UtcNow);
    }

    [Fact]
    public async Task CreateUser_WithUnverifiedEmail_ShouldPersistEmailAsUnverified()
    {
        _factory.Clock.Reset();

        using var client = CreateClient();

        var username =
            $"unverified-{Guid.NewGuid():N}";

        var email =
            $"{Guid.NewGuid():N}@example.com";

        var result = await CreateUserAsync(
            client,
            username,
            email: email,
            emailVerified: false);

        var userKey = GetUserKey(result);

        using var scope =
            _factory.Services.CreateScope();

        var factory =
            scope.ServiceProvider
                .GetRequiredService<IUserIdentifierStoreFactory>();

        var store =
            factory.Create(TenantKeys.Single);

        var identifiers =
            await store.GetByUserAsync(
                userKey);

        var identifier = identifiers
            .Single(x =>
                x.Type == UserIdentifierType.Email);

        identifier.IsVerified.Should()
            .BeFalse();

        identifier.VerifiedAt.Should()
            .BeNull();
    }

    [Fact]
    public async Task CreateUser_WithUsernameAndEmail_ShouldCreateBothIdentifiers()
    {
        _factory.Clock.Reset();

        using var client = CreateClient();

        var username =
            $"multi-{Guid.NewGuid():N}";

        var email =
            $"{Guid.NewGuid():N}@example.com";

        var result = await CreateUserAsync(
            client,
            username,
            email: email);

        var userKey = GetUserKey(result);

        using var scope =
            _factory.Services.CreateScope();

        var factory =
            scope.ServiceProvider
                .GetRequiredService<IUserIdentifierStoreFactory>();

        var store =
            factory.Create(TenantKeys.Single);

        var identifiers =
            await store.GetByUserAsync(
                userKey);

        identifiers.Should()
            .ContainSingle(x =>
                x.Type == UserIdentifierType.Username &&
                x.Value == username);

        identifiers.Should()
            .ContainSingle(x =>
                x.Type == UserIdentifierType.Email &&
                x.Value == email);
    }

    [Fact]
    public async Task CreateUser_ShouldCreateCredentialAndAllowLogin()
    {
        _factory.Clock.Reset();

        using var client = CreateClient();

        var username =
            $"credential-{Guid.NewGuid():N}";

        var secret =
            $"Test-{Guid.NewGuid():N}!";

        await CreateUserAsync(
            client,
            username,
            secret: secret);

        //
        // Do not inspect password hashes here.
        //
        // The externally observable invariant is that the
        // lifecycle integration produced a credential that
        // the authentication pipeline can consume.
        //
        var login = await client.PostAsJsonAsync(
            "/auth/login",
            new
            {
                identifier = username,
                secret
            });

        login.StatusCode.Should()
            .Be(HttpStatusCode.Found);

        login.Headers.Contains("Set-Cookie")
            .Should()
            .BeTrue();
    }

    //[Fact]
    //public async Task CreateUser_WithDuplicateUsername_ShouldNotCreateSecondUser()
    //{
    //    _factory.Clock.Reset();

    //    using var client = CreateClient();

    //    var username =
    //        $"duplicate-{Guid.NewGuid():N}";

    //    var first =
    //        await CreateUserResponseAsync(
    //            client,
    //            username);

    //    first.StatusCode.Should()
    //        .Be(HttpStatusCode.OK);

    //    var second =
    //        await CreateUserResponseAsync(
    //            client,
    //            username);

    //    var secondResult = await second.Content.ReadFromJsonAsync<UserCreateResult>();

    //    secondResult.Should().NotBeNull();

    //    secondResult!.Succeeded.Should()
    //        .BeFalse();

    //    secondResult.FailureReason.Should()
    //        .NotBeNullOrWhiteSpace();


    //    second.IsSuccessStatusCode.Should().BeFalse();

    //    // Verify the important invariant:
    //    // only one active identifier owns this username.
    //    using var scope = _factory.Services.CreateScope();

    //    var factory =
    //        scope.ServiceProvider
    //            .GetRequiredService<IUserIdentifierStoreFactory>();

    //    var store =
    //        factory.Create(TenantKeys.Single);

    //    var normalized =
    //        scope.ServiceProvider
    //            .GetRequiredService<IIdentifierNormalizer>()
    //            .Normalize(
    //                UserIdentifierType.Username,
    //                username);

    //    var identifier =
    //        await store.GetAsync(
    //            UserIdentifierType.Username,
    //            normalized.Normalized);

    //    identifier.Should().NotBeNull();
    //    identifier!.IsDeleted.Should().BeFalse();
    //}

    [Fact]
    public async Task CreateUser_ResultUserKey_ShouldMatchPersistedAggregate()
    {
        _factory.Clock.Reset();

        using var client = CreateClient();

        var username =
            $"key-{Guid.NewGuid():N}";

        var result =
            await CreateUserAsync(
                client,
                username);

        var userKey = GetUserKey(result);

        using var scope =
            _factory.Services.CreateScope();

        var lifecycleFactory =
            scope.ServiceProvider
                .GetRequiredService<IUserLifecycleStoreFactory>();

        var profileFactory =
            scope.ServiceProvider
                .GetRequiredService<IUserProfileStoreFactory>();

        var identifierFactory =
            scope.ServiceProvider
                .GetRequiredService<IUserIdentifierStoreFactory>();

        var lifecycle =
            await lifecycleFactory
                .Create(TenantKeys.Single)
                .GetAsync(
                    new UserLifecycleKey(
                        TenantKeys.Single,
                        userKey));

        var profile =
            await profileFactory
                .Create(TenantKeys.Single)
                .GetAsync(
                    new UserProfileKey(
                        TenantKeys.Single,
                        userKey,
                        ProfileKey.Default));

        var identifiers =
            await identifierFactory
                .Create(TenantKeys.Single)
                .GetByUserAsync(
                    userKey);

        lifecycle.Should().NotBeNull();
        profile.Should().NotBeNull();
        identifiers.Should().NotBeEmpty();

        lifecycle!.UserKey.Should()
            .Be(userKey);

        profile!.UserKey.Should()
            .Be(userKey);

        identifiers.Should()
            .OnlyContain(x =>
                x.UserKey == userKey);
    }


    // -------------------------------------------------------
    // Helpers
    // -------------------------------------------------------

    private HttpClient CreateClient()
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
            $"user-lifecycle-{Guid.NewGuid():N}");

        return client;
    }

    private async Task<UserCreateResult> CreateUserAsync(
        HttpClient client,
        string username,
        string? secret = null,
        string? displayName = null,
        string? email = null,
        bool emailVerified = false)
    {
        var response =
            await CreateUserResponseAsync(
                client,
                username,
                secret,
                displayName,
                email,
                emailVerified);

        response.StatusCode.Should()
            .Be(HttpStatusCode.OK);

        var result =
            await response.Content
                .ReadFromJsonAsync<UserCreateResult>();

        result.Should().NotBeNull();
        result!.Succeeded.Should().BeTrue();

        return result;
    }

    private static Task<HttpResponseMessage>
        CreateUserResponseAsync(
            HttpClient client,
            string username,
            string? secret = null,
            string? displayName = null,
            string? email = null,
            bool emailVerified = false)
    {
        secret ??=
            $"Test-{Guid.NewGuid():N}!";

        return client.PostAsJsonAsync(
            "/auth/users/create",
            new CreateUserRequest
            {
                UserName = username,
                Email = email,
                EmailVerified = emailVerified,
                DisplayName = displayName ?? username,
                Password = secret
            });
    }


    private static UserKey GetUserKey(UserCreateResult result)
    {
        result.UserKey.Should().NotBeNullOrWhiteSpace();

        return UserKey.FromString(result.UserKey!);
    }
}