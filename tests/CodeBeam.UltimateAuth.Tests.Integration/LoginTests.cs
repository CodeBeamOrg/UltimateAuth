using CodeBeam.UltimateAuth.Users.Contracts;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

namespace CodeBeam.UltimateAuth.Tests.Integration;

public class LoginTests : IClassFixture<AuthServerFactory>
{
    private readonly HttpClient _client;

    public LoginTests(AuthServerFactory factory)
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
    public async Task Login_Should_Return_Cookie()
    {
        var response = await _client.PostAsJsonAsync("/auth/login", new
        {
            identifier = "admin",
            secret = "admin"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Found);
        response.Headers.Location.Should().NotBeNull();
        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        cookies.Should().NotBeNull();
    }

    [Fact]
    public async Task Session_Lifecycle_Should_Work_Correctly()
    {
        var loginResponse1 = await _client.PostAsJsonAsync("/auth/login", new
        {
            identifier = "admin",
            secret = "admin"
        });

        loginResponse1.StatusCode.Should().Be(HttpStatusCode.Found);

        var cookie1 = loginResponse1.Headers.GetValues("Set-Cookie").FirstOrDefault();
        cookie1.Should().NotBeNull();

        _client.DefaultRequestHeaders.Add("Cookie", cookie1!);

        var logoutResponse = await _client.PostAsync("/auth/logout", null);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.Found);
        
        var logoutAgain = await _client.PostAsync("/auth/logout", null);
        logoutAgain.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Found);

        _client.DefaultRequestHeaders.Remove("Cookie");

        var loginResponse2 = await _client.PostAsJsonAsync("/auth/login", new
        {
            identifier = "admin",
            secret = "admin"
        });

        loginResponse2.StatusCode.Should().Be(HttpStatusCode.Found);
        var cookie2 = loginResponse2.Headers.GetValues("Set-Cookie").FirstOrDefault();
        cookie2.Should().NotBeNull();
        cookie2.Should().NotBe(cookie1);
    }

    [Fact]
    public async Task Authenticated_User_Should_Access_Me_Endpoint()
    {
        var loginResponse = await _client.PostAsJsonAsync("/auth/login", new
        {
            identifier = "admin",
            secret = "admin"
        });

        var cookie = loginResponse.Headers.GetValues("Set-Cookie").First();
        _client.DefaultRequestHeaders.Add("Cookie", cookie);
        var response = await _client.PostAsJsonAsync("/auth/me/profile/get", new GetProfileRequest() { ProfileKey = null });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Anonymous_Should_Not_Access_Me()
    {
        var response = await _client.PostAsync("/auth/me/profile/get", null);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}