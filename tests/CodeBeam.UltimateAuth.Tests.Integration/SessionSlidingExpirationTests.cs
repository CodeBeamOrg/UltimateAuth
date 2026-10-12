using System.Net;
using System.Net.Http.Json;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Tests.Integration.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Tests.Integration;

public sealed class SessionSlidingExpirationTests
{
    [Theory]
    [InlineData("BlazorServer")]
    [InlineData("BlazorWasm")]
    public async Task Sliding_RegularRefresh_ShouldSurviveOriginalLifetime(string profile)
    {
        await using var factory = CreateFactory(sliding: true);
        using var client = CreateClient(factory, profile);
        await Login(factory, client);
        var (initialSession, initialChain) = await ReadCurrent(factory, client);
        var previousExpiry = initialSession.ExpiresAt;
        for (int i = 0; i < 3; i++)
        {
            factory.Clock.Advance(TimeSpan.FromDays(2));
            using var response = await Refresh(client, profile);
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            var (session, chain) = await ReadCurrent(factory, client);
            chain.ChainId.Should().Be(initialChain.ChainId);

            session.ExpiresAt.Should().BeAfter(previousExpiry);
            previousExpiry = session.ExpiresAt;
        }
        factory.Clock.UtcNow.Should().BeAfter(initialSession.ExpiresAt);
    }

    [Theory]
    [InlineData("BlazorServer")]
    [InlineData("BlazorWasm")]
    public async Task SlidingDisabled_RefreshShouldNotExtendExpiration(string profile)
    {
        await using var factory = CreateFactory(sliding: false);
        using var client = CreateClient(factory, profile);
        await Login(factory, client);
        var (initial, _) = await ReadCurrent(factory, client);
        factory.Clock.Advance(TimeSpan.FromDays(2));
        using (var response = await Refresh(client, profile))
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var (after, _) = await ReadCurrent(factory, client);
        after.ExpiresAt.Should().Be(initial.ExpiresAt);
        factory.Clock.Advance(TimeSpan.FromDays(2));
        using var expired = await Refresh(client, profile);
        expired.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("BlazorServer")]
    [InlineData("BlazorWasm")]
    public async Task MaxLifetime_RefreshMustNotExtendAbsoluteChainExpiry(string profile)
    {
        await using var factory = CreateFactory(sliding: true, maxLifetime: TimeSpan.FromDays(9));
        using var client = CreateClient(factory, profile);
        await Login(factory, client);
        var (_, initialChain) = await ReadCurrent(factory, client);
        var absolute = initialChain.AbsoluteExpiresAt;
        absolute.Should().Be(factory.Clock.UtcNow.AddDays(9));
        for (int i = 0; i < 4; i++)
        {
            factory.Clock.Advance(TimeSpan.FromDays(2));
            using var response = await Refresh(client, profile);
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
            var (session, chain) = await ReadCurrent(factory, client);
            chain.AbsoluteExpiresAt.Should().Be(absolute);
            session.ExpiresAt.Should().BeOnOrBefore(absolute!.Value);
        }
        factory.Clock.Advance(TimeSpan.FromDays(1));
        using var boundary = await Refresh(client, profile);
        boundary.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PureOpaque_NoOpBeforeInterval_ThenTouchMustExtendSession()
    {
        await using var factory = CreateFactory(sliding: true);
        using var client = CreateClient(factory, "BlazorServer");
        await Login(factory, client);
        var (initial, chainBefore) = await ReadCurrent(factory, client);
        factory.Clock.Advance(TimeSpan.FromMinutes(1));
        using (var response = await Refresh(client, "BlazorServer"))
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var (noOp, chainNoOp) = await ReadCurrent(factory, client);
        noOp.ExpiresAt.Should().Be(initial.ExpiresAt);
        chainNoOp.TouchCount.Should().Be(chainBefore.TouchCount);
        factory.Clock.Advance(TimeSpan.FromDays(1));
        using (var response = await Refresh(client, "BlazorServer"))
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var (touched, chainTouched) = await ReadCurrent(factory, client);
        chainTouched.TouchCount.Should().BeGreaterThan(chainBefore.TouchCount);
        touched.ExpiresAt.Should().BeAfter(initial.ExpiresAt);
    }

    [Fact]
    public async Task Hybrid_RotatedToken_ShouldNotLeaveSessionExpiryBehind()
    {
        await using var factory = CreateFactory(sliding: true);
        using var client = CreateClient(factory, "BlazorWasm");
        await Login(factory, client);
        var (initial, chainBefore) = await ReadCurrent(factory, client);
        var oldRefreshCookie = Cookie(client, "uar");
        factory.Clock.Advance(TimeSpan.FromDays(2));
        using (var response = await Refresh(client, "BlazorWasm"))
            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        Cookie(client, "uar").Should().NotBe(oldRefreshCookie);
        var (after, chainAfter) = await ReadCurrent(factory, client);
        chainAfter.ChainId.Should().Be(chainBefore.ChainId);
        after.ExpiresAt.Should().BeAfter(initial.ExpiresAt);
    }

    [Theory]
    [InlineData("BlazorServer")]
    [InlineData("BlazorWasm")]
    public async Task IdentifierGet_ExpiredIdleSession_ShouldReturnUnauthorized(string profile)
    {
        using var factory = AuthServerFactory.Create(o =>
        {
            o.Session.Lifetime = TimeSpan.FromMinutes(5);
            o.Session.MaxLifetime = null;
            o.Session.IdleTimeout = TimeSpan.FromSeconds(10);
            o.Session.TouchInterval = TimeSpan.FromSeconds(5);
        });

        var user = await factory.CreateLoginUserAsync();

        using var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = false
            });

        client.DefaultRequestHeaders.Add(
            "Origin", "https://localhost:6130");
        client.DefaultRequestHeaders.Add(
            "X-UDID", $"idle-regression-{Guid.NewGuid():N}");
        client.DefaultRequestHeaders.Add(
            "X-UAuth-ClientProfile", profile);

        using var login = await client.PostAsJsonAsync(
            "/auth/login",
            new { identifier = user.Identifier, secret = user.Secret });

        login.StatusCode.Should().Be(HttpStatusCode.Found);

        login.Headers.TryGetValues("Set-Cookie", out var cookies)
            .Should().BeTrue();

        client.DefaultRequestHeaders.Add(
            "Cookie",
            string.Join("; ", cookies!.Select(x => x.Split(';')[0])));

        // Same session must be accepted before IdleTimeout.
        using var before = await client.PostAsJsonAsync(
            "/auth/me/identifiers/get", new { });

        before.StatusCode.Should().Be(HttpStatusCode.OK);

        // Expire idle time, not the session's normal lifetime.
        factory.Clock.Advance(TimeSpan.FromSeconds(11));

        // Keep the original cookie deliberately.
        using var after = await client.PostAsJsonAsync(
            "/auth/me/identifiers/get", new { });

        after.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "an expired session must not remain authenticated " +
            "merely because its cookie is still present");
    }


    [Theory]
    [InlineData("BlazorServer")]
    [InlineData("BlazorWasm")]
    public async Task InfiniteSliding_ThirtyConsecutiveRefreshes_ShouldKeepSessionAlive(
        string profile)
    {
        await using var factory = CreateFactory(
            sliding: true,
            maxLifetime: null);

        using var client = CreateClient(factory, profile);
        await Login(factory, client);

        var (initialSession, initialChain) =
            await ReadCurrent(factory, client);

        initialChain.AbsoluteExpiresAt.Should().BeNull();

        var previousExpiry = initialSession.ExpiresAt;
        var previousTouchCount = initialChain.TouchCount;

        for (int i = 0; i < 30; i++)
        {
            factory.Clock.Advance(TimeSpan.FromDays(2));

            using var response = await Refresh(client, profile);

            response.StatusCode.Should().Be(
                HttpStatusCode.NoContent,
                $"refresh #{i + 1} should succeed");

            var (session, chain) =
                await ReadCurrent(factory, client);

            chain.ChainId.Should().Be(initialChain.ChainId);
            chain.AbsoluteExpiresAt.Should().BeNull();

            session.ExpiresAt.Should().BeAfter(
                previousExpiry,
                $"refresh #{i + 1} must extend expiration");

            session.ExpiresAt.Should().Be(
                factory.Clock.UtcNow.AddDays(4));

            chain.TouchCount.Should().Be(
                previousTouchCount + 1);

            previousExpiry = session.ExpiresAt;
            previousTouchCount = chain.TouchCount;
        }

        factory.Clock.UtcNow.Should().BeAfter(
            initialSession.ExpiresAt);

        // 30 refreshes, each separated by 2 days.
        factory.Clock.UtcNow.Should().Be(
            initialChain.CreatedAt.AddDays(60));
    }

    [Theory]
    [InlineData("BlazorServer")]
    [InlineData("BlazorWasm")]
    public async Task Sliding_JustBeforeSessionExpiry_ShouldExtendSession(
        string profile)
    {
        await using var factory = CreateFactory(sliding: true);
        using var client = CreateClient(factory, profile);

        await Login(factory, client);

        var (initialSession, initialChain) =
            await ReadCurrent(factory, client);

        var remaining =
            initialSession.ExpiresAt
            - factory.Clock.UtcNow
            - TimeSpan.FromSeconds(1);

        remaining.Should().BePositive();

        factory.Clock.Advance(remaining);

        using var response = await Refresh(client, profile);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var (session, chain) =
            await ReadCurrent(factory, client);

        chain.ChainId.Should().Be(initialChain.ChainId);
        session.ExpiresAt.Should().BeAfter(initialSession.ExpiresAt);
    }

    [Theory]
    [InlineData("BlazorServer")]
    [InlineData("BlazorWasm")]
    public async Task Sliding_ExactlyAtSessionExpiry_ShouldRejectRefresh(
        string profile)
    {
        await using var factory = CreateFactory(sliding: true);
        using var client = CreateClient(factory, profile);

        await Login(factory, client);

        var (initialSession, _) =
            await ReadCurrent(factory, client);

        factory.Clock.Advance(
            initialSession.ExpiresAt - factory.Clock.UtcNow);

        using var response = await Refresh(client, profile);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Theory]
    [InlineData("BlazorServer")]
    [InlineData("BlazorWasm")]
    public async Task Sliding_WithIdleTimeout_RegularRefreshes_ShouldRemainActive(
        string profile)
    {
        await using var factory = CreateIdleSlidingFactory();
        using var client = CreateClient(factory, profile);

        await Login(factory, client);

        var (initialSession, initialChain) =
            await ReadCurrent(factory, client);

        var previousExpiry = initialSession.ExpiresAt;

        for (int i = 0; i < 12; i++)
        {
            // IdleTimeout = 30 minutes.
            // Refresh every 20 minutes.
            factory.Clock.Advance(TimeSpan.FromMinutes(20));

            using var response = await Refresh(client, profile);

            response.StatusCode.Should().Be(
                HttpStatusCode.NoContent,
                $"refresh #{i + 1} occurs before idle timeout");

            var (session, chain) =
                await ReadCurrent(factory, client);

            chain.ChainId.Should().Be(initialChain.ChainId);
            chain.LastSeenAt.Should().Be(factory.Clock.UtcNow);
            session.ExpiresAt.Should().BeAfter(previousExpiry);

            previousExpiry = session.ExpiresAt;
        }

        // 12 * 20 minutes = 4 hours.
        // Original session lifetime = 1 hour.
        factory.Clock.UtcNow.Should().BeAfter(
            initialSession.ExpiresAt);
    }

    [Theory]
    [InlineData("BlazorServer")]
    [InlineData("BlazorWasm")]
    public async Task Sliding_WithIdleTimeout_ExactlyAtBoundary_ShouldRejectRefresh(
        string profile)
    {
        await using var factory = CreateIdleSlidingFactory();
        using var client = CreateClient(factory, profile);

        await Login(factory, client);

        var (initialSession, initialChain) =
            await ReadCurrent(factory, client);

        factory.Clock.Advance(TimeSpan.FromMinutes(30));

        using var response = await Refresh(client, profile);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        var (session, chain) =
            await ReadCurrentDirectly(factory, initialChain);

        chain.TouchCount.Should().Be(initialChain.TouchCount);
        chain.LastSeenAt.Should().Be(initialChain.LastSeenAt);
        session.ExpiresAt.Should().Be(initialSession.ExpiresAt);
    }

    [Fact]
    public async Task Hybrid_ConcurrentRefresh_ShouldNotLeaveBrokenSessionGraph()
    {
        await using var factory = CreateFactory(sliding: true);
        using var client = CreateClient(factory, "BlazorWasm");

        await Login(factory, client);

        var (initialSession, initialChain) =
            await ReadCurrent(factory, client);

        // Both requests use the same original refresh cookie.
        // Do not apply response cookies until both requests finish.
        factory.Clock.Advance(TimeSpan.FromDays(2));

        using var firstClient = CloneClientWithCookies(
            factory, client, "BlazorWasm");

        using var secondClient = CloneClientWithCookies(
            factory, client, "BlazorWasm");

        using var first = await firstClient.PostAsync(
            "/auth/refresh", null);

        using var second = await secondClient.PostAsync(
            "/auth/refresh", null);

        // A rotated refresh credential must not be reusable.
        first.StatusCode.Should().Be(HttpStatusCode.NoContent);
        second.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        // Apply the successful rotation to the original client.
        ApplyCookies(client, first);

        var (session, chain) =
            await ReadCurrent(factory, client);

        chain.ChainId.Should().Be(initialChain.ChainId);
        chain.ActiveSessionId.Should().NotBeNull();
        session.ChainId.Should().Be(chain.ChainId);
        session.ExpiresAt.Should().BeAfter(initialSession.ExpiresAt);

        // The successful rotated credential remains usable.
        using var next = await Refresh(client, "BlazorWasm");

        next.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Theory]
    [InlineData("BlazorServer")]
    [InlineData("BlazorWasm")]
    public async Task ZeroIdleTimeout_RefreshShouldRemainValidBeforeSessionExpiry(string profile)
    {
        await using var factory = AuthServerFactory.Create(o =>
        {
            o.Session.Lifetime = TimeSpan.FromHours(1);
            o.Session.MaxLifetime = null;
            o.Session.IdleTimeout = TimeSpan.Zero;
            o.Session.TouchInterval = TimeSpan.FromMinutes(5);
            o.Session.SlidingExpiration = true;
            o.Token.RefreshTokenLifetime = TimeSpan.FromDays(7);
        });

        using var client = CreateClient(factory, profile);
        await Login(factory, client);

        factory.Clock.Advance(TimeSpan.FromMinutes(30));

        using var response = await Refresh(client, profile);
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }


    private static AuthServerFactory CreateFactory(bool sliding, TimeSpan? maxLifetime = null)
    {
        var factory = AuthServerFactory.Create(o =>
        {
            o.Session.Lifetime = TimeSpan.FromDays(4);
            o.Session.SlidingExpiration = sliding;
            o.Session.MaxLifetime = maxLifetime;
            o.Session.IdleTimeout = null;
            o.Token.RefreshTokenLifetime = TimeSpan.FromDays(12);
        });
        factory.Clock.Reset();
        return factory;
    }

    private static HttpClient CreateClient(AuthServerFactory factory, string profile)
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = false
        });
        client.DefaultRequestHeaders.Add("Origin", "https://localhost:6130");
        client.DefaultRequestHeaders.Add("X-UDID", $"sliding-device-{Guid.NewGuid():N}");
        client.DefaultRequestHeaders.Add("X-UAuth-ClientProfile", profile);
        return client;
    }

    private static async Task Login(AuthServerFactory factory, HttpClient client)
    {
        var user = await factory.CreateLoginUserAsync();
        using var response = await client.PostAsJsonAsync("/auth/login",
            new { identifier = user.Identifier, secret = user.Secret });
        response.StatusCode.Should().Be(HttpStatusCode.Found);
        ApplyCookies(client, response);
    }

    private static async Task<HttpResponseMessage> Refresh(HttpClient client, string profile)
    {
        var response = await client.PostAsync("/auth/refresh", null);
        if (response.IsSuccessStatusCode && profile == "BlazorWasm")
            ApplyCookies(client, response);
        return response;
    }

    private static void ApplyCookies(HttpClient client, HttpResponseMessage response)
    {
        var cookies = new Dictionary<string, string>(StringComparer.Ordinal);
        if (client.DefaultRequestHeaders.TryGetValues("Cookie", out var old))
            foreach (var item in string.Join("; ", old).Split("; ", StringSplitOptions.RemoveEmptyEntries))
            {
                var pair = item.Split('=', 2);
                if (pair.Length == 2) cookies[pair[0]] = pair[1];
            }
        if (response.Headers.TryGetValues("Set-Cookie", out var values))
            foreach (var item in values)
            {
                var pair = item.Split(';', 2)[0].Split('=', 2);
                if (pair.Length == 2) cookies[pair[0]] = pair[1];
            }
        client.DefaultRequestHeaders.Remove("Cookie");
        client.DefaultRequestHeaders.Add("Cookie", string.Join("; ", cookies.Select(x => $"{x.Key}={x.Value}")));
    }

    private static string Cookie(HttpClient client, string name) =>
        client.DefaultRequestHeaders.GetValues("Cookie").Single()
            .Split("; ", StringSplitOptions.RemoveEmptyEntries)
            .Single(x => x.StartsWith(name + "=", StringComparison.Ordinal));

    private static async Task<(UAuthSession Session, UAuthSessionChain Chain)> ReadCurrent(
        AuthServerFactory factory, HttpClient client)
    {
        using var response = await client.PostAsJsonAsync("/auth/me/sessions/chains",
            new PageRequest { PageNumber = 1, PageSize = 50 });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await response.Content.ReadFromJsonAsync<PagedResult<SessionChainSummary>>();
        var chainId = page!.Items.Single(x => x.IsCurrentDevice).ChainId;
        using var scope = factory.Services.CreateScope();
        var store = scope.ServiceProvider.GetRequiredService<ISessionStoreFactory>().Create(TenantKeys.Single);
        var chain = await store.GetChainAsync(chainId);
        chain.Should().NotBeNull();
        chain!.ActiveSessionId.Should().NotBeNull();
        var session = await store.GetSessionAsync(chain.ActiveSessionId!.Value);
        session.Should().NotBeNull();
        return (session!, chain);
    }


    private static AuthServerFactory CreateIdleSlidingFactory()
    {
        var factory = AuthServerFactory.Create(o =>
        {
            o.Session.Lifetime = TimeSpan.FromHours(1);
            o.Session.MaxLifetime = null;
            o.Session.SlidingExpiration = true;
            o.Session.IdleTimeout = TimeSpan.FromMinutes(30);
            o.Session.TouchInterval = TimeSpan.FromMinutes(5);

            o.Token.RefreshTokenLifetime = TimeSpan.FromDays(7);
        });

        factory.Clock.Reset();
        return factory;
    }

    private static async Task<(UAuthSession Session, UAuthSessionChain Chain)> ReadCurrentDirectly(AuthServerFactory factory, UAuthSessionChain originalChain)
    {
        using var scope = factory.Services.CreateScope();

        var store = scope.ServiceProvider
            .GetRequiredService<ISessionStoreFactory>()
            .Create(TenantKeys.Single);

        var chain = await store.GetChainAsync(originalChain.ChainId);

        chain.Should().NotBeNull();
        chain!.ActiveSessionId.Should().NotBeNull();

        var session = await store.GetSessionAsync(
            chain.ActiveSessionId!.Value);

        session.Should().NotBeNull();

        return (session!, chain);
    }

    private static HttpClient CloneClientWithCookies(AuthServerFactory factory, HttpClient source, string profile)
    {
        var client = factory.CreateClient(
            new WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
                HandleCookies = false
            });

        foreach (var header in source.DefaultRequestHeaders)
        {
            client.DefaultRequestHeaders.TryAddWithoutValidation(
                header.Key,
                header.Value);
        }

        return client;
    }
}
