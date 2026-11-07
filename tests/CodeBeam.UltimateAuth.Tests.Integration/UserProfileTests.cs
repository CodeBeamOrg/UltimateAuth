using CodeBeam.UltimateAuth.Tests.Integration.Infrastructure;
using CodeBeam.UltimateAuth.Users.Contracts;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

namespace CodeBeam.UltimateAuth.Tests.Integration;

public class UserProfileTests : IDisposable
{
    // Profiles created by one test must not exist in another test's host.
    private readonly AuthServerFactory _factory = new();
    private readonly HttpClient _client;

    public UserProfileTests()
    {
        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        _client.DefaultRequestHeaders.Add("Origin", "https://localhost:6130");
        _client.DefaultRequestHeaders.Add("X-UDID", $"profile-test-{Guid.NewGuid():N}");
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    [Fact]
    public async Task Profile_Switch_Should_Return_Correct_Profile_Data()
    {
        await Login();

        var defaultResponse = await PostProfileAsync("/auth/me/profile/get", new { });
        defaultResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var defaultProfile = await defaultResponse.Content.ReadFromJsonAsync<UserView>(cancellationToken: TestContext.Current.CancellationToken);
        defaultProfile.Should().NotBeNull();

        var createResponse = await PostProfileAsync("/auth/me/profile/create", new CreateProfileRequest
        {
            ProfileKey = ProfileKey.Parse("business", null)
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updateResponse = await PostProfileAsync("/auth/me/profile/update", new UpdateProfileRequest()
        {
            ProfileKey = ProfileKey.Parse("business", null),
            DisplayName = "Updated Business Name"
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var businessResponse = await PostProfileAsync("/auth/me/profile/get", new GetProfileRequest()
        {
            ProfileKey = ProfileKey.Parse("business", null)
        });

        businessResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var businessProfile = await businessResponse.Content.ReadFromJsonAsync<UserView>(cancellationToken: TestContext.Current.CancellationToken);

        businessProfile.Should().NotBeNull();
        businessProfile!.DisplayName.Should().Be("Updated Business Name");

        var defaultAgainResponse = await PostProfileAsync("/auth/me/profile/get", new { });

        var defaultAgain = await defaultAgainResponse.Content.ReadFromJsonAsync<UserView>(cancellationToken: TestContext.Current.CancellationToken);

        defaultAgain!.DisplayName.Should().Be(defaultProfile!.DisplayName);
    }

    [Fact]
    public async Task GetMe_Without_ProfileKey_Should_Return_Default_Profile()
    {
        await Login();

        var response = await _client.PostAsJsonAsync("/auth/me/profile/get", new { }, cancellationToken: TestContext.Current.CancellationToken);

        var profile = await response.Content.ReadFromJsonAsync<UserView>(cancellationToken: TestContext.Current.CancellationToken);

        profile.Should().NotBeNull();
        profile!.ProfileKey.Value.Should().Be("default");
    }

    [Fact]
    public async Task Should_Not_Found_NonDefault_Profile_When_Not_Created()
    {
        await Login();

        var response = await PostProfileAsync("/auth/me/profile/get", new
        {
            profileKey = "business"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Should_Not_Create_Duplicate_Profile()
    {
        var login = await Login();

        var key = ProfileKey.Parse($"business-{Guid.NewGuid()}", null);

        var cookie = login.Headers.GetValues("Set-Cookie").First();
        _client.DefaultRequestHeaders.Add("Cookie", cookie);

        var request = new CreateProfileRequest
        {
            ProfileKey = key
        };

        var first = await PostProfileAsync("/auth/me/profile/create", request);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await PostProfileAsync("/auth/me/profile/create", request);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Should_Not_Delete_Default_Profile()
    {
        await Login();

        var response = await PostProfileAsync("/auth/me/profile/delete", new
        {
            profileKey = "default"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Should_Not_Update_NonExisting_Profile()
    {
        await Login();

        var response = await PostProfileAsync("/auth/me/profile/update", new UpdateProfileRequest
        {
            ProfileKey = ProfileKey.Parse("ghost", null),
            DisplayName = "Should Fail"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Deleted_Profile_Should_Not_Be_Returned()
    {
        await Login();

        var key = ProfileKey.Parse("business", null);

        await PostProfileAsync("/auth/me/profile/create", new CreateProfileRequest
        {
            ProfileKey = key
        });

        await PostProfileAsync("/auth/me/profile/delete", new
        {
            profileKey = key.Value
        });

        var response = await PostProfileAsync("/auth/me/profile/get", new GetProfileRequest
        {
            ProfileKey = key
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }


    private async Task<HttpResponseMessage> Login()
    {
        var response = await CsrfTestClient.PostJsonAsync(
            _client,
            "/auth/login",
            new
            {
                identifier = "admin",
                secret = "admin"
            });

        response.StatusCode.Should().Be(HttpStatusCode.Found);

        response.Headers.TryGetValues(
            "Set-Cookie",
            out var cookies).Should().BeTrue();

        var sessionCookie = cookies!
            .Single(x => x.StartsWith(
                "uas=",
                StringComparison.OrdinalIgnoreCase))
            .Split(';', 2)[0];

        _client.DefaultRequestHeaders.Remove("Cookie");
        _client.DefaultRequestHeaders.Add("Cookie", sessionCookie);

        return response;
    }

    private Task<HttpResponseMessage> PostProfileAsync<T>(
        string url,
        T request)
    {
        return CsrfTestClient.PostJsonAsync(
            _client,
            url,
            request);
    }
}
