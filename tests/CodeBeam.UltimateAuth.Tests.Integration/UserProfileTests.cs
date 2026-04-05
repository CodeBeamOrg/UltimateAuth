using CodeBeam.UltimateAuth.Users.Contracts;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

namespace CodeBeam.UltimateAuth.Tests.Integration;

public class UserProfileTests : IClassFixture<AuthServerFactory>
{
    private readonly HttpClient _client;

    public UserProfileTests(AuthServerFactory factory)
    {
        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        _client.DefaultRequestHeaders.Add("Origin", "https://localhost:6130");
        _client.DefaultRequestHeaders.Add("X-UDID", "test-device-1234567890123456");
    }

    [Fact]
    public async Task Profile_Switch_Should_Return_Correct_Profile_Data()
    {
        var loginResponse = await _client.PostAsJsonAsync("/auth/login", new
        {
            identifier = "admin",
            secret = "admin"
        });

        var cookie = loginResponse.Headers.GetValues("Set-Cookie").First();
        _client.DefaultRequestHeaders.Add("Cookie", cookie);

        var defaultResponse = await _client.PostAsJsonAsync("/auth/me/profile/get", new { });
        defaultResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var defaultProfile = await defaultResponse.Content.ReadFromJsonAsync<UserView>();
        defaultProfile.Should().NotBeNull();

        var createResponse = await _client.PostAsJsonAsync("/auth/me/profile/create", new CreateProfileRequest
        {
            ProfileKey = ProfileKey.Parse("business", null)
        });

        createResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updateResponse = await _client.PostAsJsonAsync("/auth/me/profile/update", new UpdateProfileRequest()
        {
            ProfileKey = ProfileKey.Parse("business", null),
            DisplayName = "Updated Business Name"
        });

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var businessResponse = await _client.PostAsJsonAsync("/auth/me/profile/get", new GetProfileRequest()
        {
            ProfileKey = ProfileKey.Parse("business", null)
        });

        businessResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var businessProfile = await businessResponse.Content.ReadFromJsonAsync<UserView>();

        businessProfile.Should().NotBeNull();
        businessProfile!.DisplayName.Should().Be("Updated Business Name");

        var defaultAgainResponse = await _client.PostAsJsonAsync("/auth/me/profile/get", new { });

        var defaultAgain = await defaultAgainResponse.Content.ReadFromJsonAsync<UserView>();

        defaultAgain!.DisplayName.Should().Be(defaultProfile!.DisplayName);
    }

    [Fact]
    public async Task GetMe_Without_ProfileKey_Should_Return_Default_Profile()
    {
        var login = await _client.PostAsJsonAsync("/auth/login", new
        {
            identifier = "admin",
            secret = "admin"
        });

        var cookie = login.Headers.GetValues("Set-Cookie").First();
        _client.DefaultRequestHeaders.Add("Cookie", cookie);

        var response = await _client.PostAsJsonAsync("/auth/me/profile/get", new { });

        var profile = await response.Content.ReadFromJsonAsync<UserView>();

        profile.Should().NotBeNull();
        profile!.ProfileKey.Value.Should().Be("default");
    }

    [Fact]
    public async Task Should_Not_Found_NonDefault_Profile_When_Not_Created()
    {
        var login = await _client.PostAsJsonAsync("/auth/login", new
        {
            identifier = "admin",
            secret = "admin"
        });

        var cookie = login.Headers.GetValues("Set-Cookie").First();
        _client.DefaultRequestHeaders.Add("Cookie", cookie);

        var response = await _client.PostAsJsonAsync("/auth/me/profile/get", new
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

        var first = await _client.PostAsJsonAsync("/auth/me/profile/create", request);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await _client.PostAsJsonAsync("/auth/me/profile/create", request);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Should_Not_Delete_Default_Profile()
    {
        var login = await Login();
        var cookie = login.Headers.GetValues("Set-Cookie").First();
        _client.DefaultRequestHeaders.Add("Cookie", cookie);

        var response = await _client.PostAsJsonAsync("/auth/me/profile/delete", new
        {
            profileKey = "default"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Should_Not_Update_NonExisting_Profile()
    {
        var login = await Login();
        var cookie = login.Headers.GetValues("Set-Cookie").First();
        _client.DefaultRequestHeaders.Add("Cookie", cookie);

        var response = await _client.PostAsJsonAsync("/auth/me/profile/update", new UpdateProfileRequest
        {
            ProfileKey = ProfileKey.Parse("ghost", null),
            DisplayName = "Should Fail"
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Deleted_Profile_Should_Not_Be_Returned()
    {
        var login = await Login();
        var cookie = login.Headers.GetValues("Set-Cookie").First();
        _client.DefaultRequestHeaders.Add("Cookie", cookie);

        var key = ProfileKey.Parse("business", null);

        await _client.PostAsJsonAsync("/auth/me/profile/create", new CreateProfileRequest
        {
            ProfileKey = key
        });

        await _client.PostAsJsonAsync("/auth/me/profile/delete", new
        {
            profileKey = key.Value
        });

        var response = await _client.PostAsJsonAsync("/auth/me/profile/get", new GetProfileRequest
        {
            ProfileKey = key
        });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }


    private async Task<HttpResponseMessage> Login()
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new
        {
            identifier = "admin",
            secret = "admin"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Found);

        var cookie = response.Headers.GetValues("Set-Cookie").First();

        _client.DefaultRequestHeaders.Remove("Cookie");
        _client.DefaultRequestHeaders.Add("Cookie", cookie);

        return response;
    }
}
