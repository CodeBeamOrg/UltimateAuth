using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Users.Contracts;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

namespace CodeBeam.UltimateAuth.Tests.Integration;

public class LogoutTests : IClassFixture<AuthServerFactory>
{
    private readonly HttpClient _client;
    AuthServerFactory _factory;

    public LogoutTests(AuthServerFactory factory)
    {
        _factory = factory;

        _client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        _client.DefaultRequestHeaders.Add("Origin", "https://localhost:6130");
        _client.DefaultRequestHeaders.Add("X-UDID", "test-device-1234567890123456");
    }

    [Fact]
    public async Task Logout_Should_Invalidate_Session_And_Cookie()
    {
        var loginResponse = await _client.PostAsJsonAsync("/auth/login", new
        {
            identifier = "admin",
            secret = "admin"
        });

        var cookie = loginResponse.Headers.GetValues("Set-Cookie").First();

        _client.DefaultRequestHeaders.Add("Cookie", cookie);

        var logoutResponse = await _client.PostAsync("/auth/logout", null);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.Found);

        var meResponse = await _client.PostAsync("/auth/me/profile/get", null);

        meResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_Should_Detach_Chain_And_Reattach_On_Next_Login()
    {
        var loginResponse1 = await _client.PostAsJsonAsync("/auth/login", new
        {
            identifier = "admin",
            secret = "admin"
        });

        loginResponse1.StatusCode.Should().Be(HttpStatusCode.Found);

        var cookie1 = loginResponse1.Headers.GetValues("Set-Cookie").First();
        cookie1.Should().NotBeNullOrWhiteSpace();

        _client.DefaultRequestHeaders.Remove("Cookie");
        _client.DefaultRequestHeaders.Add("Cookie", cookie1);

        var logoutResponse = await _client.PostAsync("/auth/logout", null);
        logoutResponse.StatusCode.Should().Be(HttpStatusCode.Found);

        var unauthorizedChainsResponse = await _client.PostAsJsonAsync(
            "/auth/me/sessions/chains",
            new PageRequest());

        unauthorizedChainsResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        _client.DefaultRequestHeaders.Remove("Cookie");

        var loginResponse2 = await _client.PostAsJsonAsync("/auth/login", new
        {
            identifier = "admin",
            secret = "admin"
        });

        loginResponse2.StatusCode.Should().Be(HttpStatusCode.Found);

        var cookie2 = loginResponse2.Headers.GetValues("Set-Cookie").First();
        cookie2.Should().NotBeNullOrWhiteSpace();

        _client.DefaultRequestHeaders.Add("Cookie", cookie2);

        var chainsResponse = await _client.PostAsJsonAsync(
            "/auth/me/sessions/chains",
            new PageRequest
            {
                PageNumber = 1,
                PageSize = 50
            });

        chainsResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await chainsResponse.Content.ReadFromJsonAsync<PagedResult<SessionChainSummary>>();
        page.Should().NotBeNull();
        page!.Items.Should().NotBeEmpty();

        var activeChains = page.Items.Where(x => x.ActiveSessionId != null).ToList();
        activeChains.Should().HaveCount(1);

        var active = activeChains.Single();
        active.IsCurrentDevice.Should().BeTrue();
        active.IsRevoked.Should().BeFalse();
        active.ActiveSessionId.Should().NotBeNull();
    }

    [Fact]
    public async Task Logout_From_One_Device_Should_Not_Affect_Other_Device()
    {
        var client1 = CreateClient("device-111111111111111111111111111111111111111111111111111111111111111");

        var login1 = await client1.PostAsJsonAsync("/auth/login", new
        {
            identifier = "admin",
            secret = "admin"
        });

        var cookie1 = login1.Headers.GetValues("Set-Cookie").First();
        client1.DefaultRequestHeaders.Add("Cookie", cookie1);

        var client2 = CreateClient("device-2222222222222222222222222222222222222222222222222222222222222222");

        var login2 = await client2.PostAsJsonAsync("/auth/login", new
        {
            identifier = "admin",
            secret = "admin"
        });

        var cookie2 = login2.Headers.GetValues("Set-Cookie").First();
        client2.DefaultRequestHeaders.Add("Cookie", cookie2);

        await client1.PostAsync("/auth/logout", null);

        var me1 = await client1.PostAsJsonAsync("/auth/me/profile/get", new GetProfileRequest() { ProfileKey = ProfileKey.Default });
        me1.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var me2 = await client2.PostAsJsonAsync("/auth/me/profile/get", new GetProfileRequest() { ProfileKey = ProfileKey.Default });
        me2.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_Should_Not_Revoke_Chain()
    {
        var login = await _client.PostAsJsonAsync("/auth/login", new
        {
            identifier = "admin",
            secret = "admin"
        });

        var cookie = login.Headers.GetValues("Set-Cookie").First();
        _client.DefaultRequestHeaders.Add("Cookie", cookie);

        await _client.PostAsync("/auth/logout", null);

        _client.DefaultRequestHeaders.Remove("Cookie");

        var login2 = await _client.PostAsJsonAsync("/auth/login", new
        {
            identifier = "admin",
            secret = "admin"
        });

        var cookie2 = login2.Headers.GetValues("Set-Cookie").First();
        _client.DefaultRequestHeaders.Add("Cookie", cookie2);

        var chainsResponse = await _client.PostAsJsonAsync("/auth/me/sessions/chains", new PageRequest());
        var page = await chainsResponse.Content.ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        page!.Items.Should().Contain(x => x.IsCurrentDevice);

        var chain = page.Items.Single();

        chain.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public async Task Logout_Should_Clear_Only_Current_Chain()
    {
        var client1 = CreateClient("device-111111111111111111111111111111111111111111111111111111111");
        var client2 = CreateClient("device-222222222222222222222222222222222222222222222222222222222");

        var cookie1 = await LoginAndGetCookie(client1);
        var cookie2 = await LoginAndGetCookie(client2);

        client1.DefaultRequestHeaders.Add("Cookie", cookie1);
        client2.DefaultRequestHeaders.Add("Cookie", cookie2);

        await client1.PostAsync("/auth/logout", null);

        var chainsResponse = await client2.PostAsJsonAsync("/auth/me/sessions/chains", new PageRequest());
        var page = await chainsResponse.Content.ReadFromJsonAsync<PagedResult<SessionChainSummary>>();

        page!.Items.Count(x => x.ActiveSessionId != null).Should().Be(1);

        var device2Chain = page.Items.First(x => x.IsCurrentDevice);

        device2Chain.ActiveSessionId.Should().NotBeNull();
    }

    [Fact]
    public async Task Logout_Should_Delete_Cookie()
    {
        var login = await _client.PostAsJsonAsync("/auth/login", new
        {
            identifier = "admin",
            secret = "admin"
        });

        var cookie = login.Headers.GetValues("Set-Cookie").First();
        _client.DefaultRequestHeaders.Add("Cookie", cookie);

        var logout = await _client.PostAsync("/auth/logout", null);

        logout.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();

        var logoutCookie = cookies!.First();

        logoutCookie.Should().Contain("expires=");
    }

    [Fact]
    public async Task Logout_Twice_Should_Be_Idempotent()
    {
        var loginResponse = await _client.PostAsJsonAsync("/auth/login", new
        {
            identifier = "admin",
            secret = "admin"
        });

        var cookie = loginResponse.Headers.GetValues("Set-Cookie").First();
        _client.DefaultRequestHeaders.Add("Cookie", cookie);

        var first = await _client.PostAsync("/auth/logout", null);
        var second = await _client.PostAsync("/auth/logout", null);

        second.StatusCode.Should().BeOneOf(HttpStatusCode.Unauthorized, HttpStatusCode.Found);
    }


    private HttpClient CreateClient(string deviceId)
    {
        var client = _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });

        client.DefaultRequestHeaders.Add("Origin", "https://localhost:6130");
        client.DefaultRequestHeaders.Add("X-UDID", deviceId);

        return client;
    }

    private async Task<string> LoginAndGetCookie(HttpClient client)
    {
        var response = await client.PostAsJsonAsync("/auth/login", new
        {
            identifier = "admin",
            secret = "admin"
        });

        return response.Headers.GetValues("Set-Cookie").First();
    }
}
