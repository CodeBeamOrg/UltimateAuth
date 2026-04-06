using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

namespace CodeBeam.UltimateAuth.Tests.Integration;

public class RefreshTests : IClassFixture<AuthServerFactory>
{
    private readonly HttpClient _client;

    public RefreshTests(AuthServerFactory factory)
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
    public async Task Refresh_PureOpaque_Should_Touch_Session()
    {
        await LoginAsync("BlazorServer");
        var response = await RefreshAsync();

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
    }

    [Fact]
    public async Task Refresh_PureOpaque_Invalid_Should_Return_Unauthorized()
    {
        SetClientProfile("BlazorServer");
        var response = await RefreshAsync();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_Hybrid_Should_Rotate_Tokens()
    {
        await LoginAsync("BlazorWasm");

        var response = await RefreshAsync();

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
        cookies.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Refresh_Hybrid_Should_Fail_On_Reuse()
    {
        await LoginAsync("BlazorWasm");

        var first = await RefreshAsync();
        first.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var second = await RefreshAsync();

        second.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_PureOpaque_Should_Not_Touch_Immediately()
    {
        await LoginAsync("BlazorServer");

        var first = await RefreshAsync();
        first.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var second = await RefreshAsync();

        second.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Refresh_Hybrid_Should_Fail_When_Session_Mismatch()
    {
        var factory = new AuthServerFactory();

        var client1 = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        var client2 = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        var cookie1 = await LoginAsync(client1, "BlazorWasm", "device-1-1234567890123456");
        var cookie2 = await LoginAsync(client2, "BlazorWasm", "device-2-1234567890123456");

        cookie1.Should().NotBeNullOrWhiteSpace();
        cookie2.Should().NotBeNullOrWhiteSpace();
        cookie1.Should().NotBe(cookie2);

        client2.DefaultRequestHeaders.Remove("Cookie");
        client2.DefaultRequestHeaders.Add("Cookie", cookie1);
        var response = await client2.PostAsync("/auth/refresh", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_Hybrid_Should_Fail_Without_RefreshToken()
    {
        await LoginAsync("BlazorWasm");
        var cookies = _client.DefaultRequestHeaders.GetValues("Cookie").First();
        var onlySession = string.Join("; ", cookies.Split("; ").Where(x => x.StartsWith("uas=")));

        _client.DefaultRequestHeaders.Remove("Cookie");
        _client.DefaultRequestHeaders.Add("Cookie", onlySession);

        var response = await _client.PostAsync("/auth/refresh", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }


    private async Task LoginAsync(string profile)
    {
        SetClientProfile(profile);

        var response = await _client.PostAsJsonAsync("/auth/login", new
        {
            identifier = "admin",
            secret = "admin"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Found);

        var cookies = response.Headers.GetValues("Set-Cookie")
            .Select(x => x.Split(';')[0]);

        var cookieHeader = string.Join("; ", cookies);

        _client.DefaultRequestHeaders.Remove("Cookie");
        _client.DefaultRequestHeaders.Add("Cookie", cookieHeader);
    }

    private async Task<string> LoginAsync(HttpClient client, string profile, string udid = "test-device-1234567890123456")
    {
        client.DefaultRequestHeaders.Remove("Origin");
        client.DefaultRequestHeaders.Add("Origin", "https://localhost:6130");

        client.DefaultRequestHeaders.Remove("X-UDID");
        client.DefaultRequestHeaders.Add("X-UDID", udid);

        SetClientProfile(client, profile);

        var response = await client.PostAsJsonAsync("/auth/login", new
        {
            identifier = "admin",
            secret = "admin"
        });

        response.StatusCode.Should().Be(HttpStatusCode.Found);

        var cookieHeader = BuildCookieHeader(response);

        client.DefaultRequestHeaders.Remove("Cookie");
        client.DefaultRequestHeaders.Add("Cookie", cookieHeader);

        return cookieHeader;
    }

    private void SetClientProfile(string profile)
    {
        _client.DefaultRequestHeaders.Remove("X-UAuth-ClientProfile");
        _client.DefaultRequestHeaders.Add("X-UAuth-ClientProfile", profile);
    }

    private void SetClientProfile(HttpClient client, string profile)
    {
        client.DefaultRequestHeaders.Remove("X-UAuth-ClientProfile");
        client.DefaultRequestHeaders.Add("X-UAuth-ClientProfile", profile);
    }

    private static string BuildCookieHeader(HttpResponseMessage response)
    {
        var cookies = response.Headers.GetValues("Set-Cookie")
            .Select(x => x.Split(';')[0]);

        return string.Join("; ", cookies);
    }

    private Task<HttpResponseMessage> RefreshAsync()
    {
        return _client.PostAsync("/auth/refresh", null);
    }
}
