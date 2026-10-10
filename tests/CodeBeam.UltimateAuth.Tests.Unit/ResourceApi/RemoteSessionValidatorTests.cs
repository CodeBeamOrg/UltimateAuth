using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.Infrastructure;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server.ResourceApi;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server.ResourceApi;

public sealed class RemoteSessionValidatorTests
{
    private const string SessionValue =
        "session-00000000000000000000000000000001";

    private static readonly TenantKey Tenant =
        TenantKey.FromExternal("tenant-a");

    [Fact]
    public async Task ValidateSessionAsync_ShouldPostSessionAndTenantToValidationEndpoint()
    {
        var handler = new TestHttpMessageHandler
        {
            Response = CreateFailureResponse(HttpStatusCode.Unauthorized)
        };

        var sut = CreateSut(handler);

        var context = CreateValidationContext();

        await sut.ValidateSessionAsync(context);

        handler.Method.Should().Be(HttpMethod.Post);

        handler.RequestUri.Should()
            .Be(new Uri("https://uauth.test/auth/validate"));

        handler.Body.Should().NotBeNullOrWhiteSpace();

        using var json = JsonDocument.Parse(handler.Body!);

        json.RootElement
            .GetProperty("sessionId")
            .GetString()
            .Should()
            .Be(context.SessionId.Value);

        json.RootElement
            .GetProperty("tenant")
            .GetString()
            .Should()
            .Be(Tenant.Value);
    }

    [Fact]
    public async Task ValidateSessionAsync_WhenIncomingRequestContainsCookie_ShouldForwardCookie()
    {
        var handler = new TestHttpMessageHandler
        {
            Response = CreateFailureResponse(HttpStatusCode.Unauthorized)
        };

        var httpContext = new DefaultHttpContext();

        httpContext.Request.Headers.Cookie =
            "uauth_session=session-cookie; other=value";

        var sut = CreateSut(
            handler,
            httpContext);

        await sut.ValidateSessionAsync(
            CreateValidationContext());

        handler.Cookie.Should()
            .Be("uauth_session=session-cookie; other=value");
    }

    [Fact]
    public async Task ValidateSessionAsync_WhenIncomingRequestDoesNotContainCookie_ShouldNotAddCookieHeader()
    {
        var handler = new TestHttpMessageHandler
        {
            Response = CreateFailureResponse(HttpStatusCode.Unauthorized)
        };

        var sut = CreateSut(handler);

        await sut.ValidateSessionAsync(
            CreateValidationContext());

        handler.Cookie.Should().BeNull();
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]
    [InlineData(HttpStatusCode.Unauthorized)]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ValidateSessionAsync_WhenHubReturnsNonSuccess_ShouldReturnNotFound(
        HttpStatusCode statusCode)
    {
        var handler = new TestHttpMessageHandler
        {
            Response = CreateFailureResponse(statusCode)
        };

        var sut = CreateSut(handler);

        var context = CreateValidationContext();

        var result =
            await sut.ValidateSessionAsync(context);

        result.IsValid.Should().BeFalse();
        result.State.Should().Be(SessionState.NotFound);

        result.SessionId.Should()
            .Be(context.SessionId);
    }

    [Fact]
    public async Task ValidateSessionAsync_WhenHubReturnsJsonNull_ShouldReturnNotFound()
    {
        var handler = new TestHttpMessageHandler
        {
            Response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create<AuthValidationResult?>(null)
            }
        };

        var sut = CreateSut(handler);

        var context = CreateValidationContext();

        var result =
            await sut.ValidateSessionAsync(context);

        result.IsValid.Should().BeFalse();
        result.State.Should().Be(SessionState.NotFound);

        result.SessionId.Should()
            .Be(context.SessionId);
    }

    [Fact]
    public async Task ValidateSessionAsync_WhenHubReturnsInvalidResult_ShouldMapResult()
    {
        var dto = new AuthValidationResult
        {
            State = SessionState.Revoked,
            Snapshot = null
        };

        var handler = new TestHttpMessageHandler
        {
            Response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(dto)
            }
        };

        var sut = CreateSut(handler);

        var result =
            await sut.ValidateSessionAsync(
                CreateValidationContext());

        result.IsValid.Should().BeFalse();
        result.State.Should().Be(SessionState.Revoked);
    }

    [Fact]
    public async Task ValidateSessionAsync_WhenCancelled_ShouldPropagateCancellation()
    {
        var handler = new TestHttpMessageHandler
        {
            OnSendAsync = async (_, ct) =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, ct);

                return new HttpResponseMessage(HttpStatusCode.OK);
            }
        };

        var sut = CreateSut(handler);

        using var cts = new CancellationTokenSource();

        var task = sut.ValidateSessionAsync(
            CreateValidationContext(),
            cts.Token);

        cts.Cancel();

        var act = async () => await task;

        await act.Should()
            .ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ValidateSessionAsync_WhenHubReturnsValidSnapshot_ShouldReturnActiveResult()
    {
        var authenticatedAt =
            new DateTimeOffset(
                2026, 1, 1, 10, 30, 0,
                TimeSpan.Zero);

        var sessionId =
            AuthSessionId.Parse(SessionValue, null);

        var chainId = SessionChainId.New();
        var rootId = SessionRootId.New();

        var dto = new AuthValidationResult
        {
            State = SessionState.Active,
            ChainId = chainId,
            RootId = rootId,

            Snapshot = new AuthStateSnapshot
            {
                Identity = new AuthIdentitySnapshot
                {
                    Tenant = Tenant,
                    UserKey = UserKey.Parse("user-123", null),
                    AuthenticatedAt = authenticatedAt
                },
                Claims = ClaimsSnapshot.Empty
            }
        };

        var handler = new TestHttpMessageHandler
        {
            Response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(dto)
            }
        };

        var sut = CreateSut(handler);

        var result = await sut.ValidateSessionAsync(
            CreateValidationContext());

        result.IsValid.Should().BeTrue();
        result.State.Should().Be(SessionState.Active);

        result.Tenant.Should().Be(Tenant);
        result.UserKey.Should()
            .Be(UserKey.Parse("user-123", null));

        result.SessionId.Should().Be(sessionId);
        result.ChainId.Should().Be(chainId);
        result.RootId.Should().Be(rootId);

        result.AuthenticatedAt.Should().Be(authenticatedAt);
        result.Claims.Should().NotBeNull();
    }


    [Fact]
    public async Task ValidateSessionAsync_WhenHubReturnsValidWithoutIdentity_ShouldReturnInvalid()
    {
        var dto = new AuthValidationResult
        {
            State = SessionState.Active,
            ChainId = SessionChainId.New(),
            RootId = SessionRootId.New(),

            Snapshot = new AuthStateSnapshot
            {
                Identity = null!,
                Claims = ClaimsSnapshot.Empty
            }
        };

        var handler = new TestHttpMessageHandler
        {
            Response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(dto)
            }
        };

        var sut = CreateSut(handler);

        var result = await sut.ValidateSessionAsync(
            CreateValidationContext());

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateSessionAsync_WhenHubReturnsValidWithoutSnapshot_ShouldReturnInvalid()
    {
        var dto = new AuthValidationResult
        {
            State = SessionState.Active,
            ChainId = SessionChainId.New(),
            RootId = SessionRootId.New(),

            Snapshot = null
        };

        var handler = new TestHttpMessageHandler
        {
            Response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(dto)
            }
        };

        var sut = CreateSut(handler);

        var result = await sut.ValidateSessionAsync(
            CreateValidationContext());

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void ToDomain_WhenIsValidFalseButStateActive_ShouldFailClosed()
    {
        var dto = new AuthValidationResult
        {
            State = SessionState.Active,
        };

        var result = SessionValidationMapper.ToDomain(dto, AuthSessionId.Parse(SessionValue, null));

        result.IsValid.Should().BeFalse();
        result.State.Should().NotBe(SessionState.Active);
    }

    [Fact]
    public void ToDomain_WhenIsValidTrueButStateIsRevoked_ShouldFailClosed()
    {
        var dto = new AuthValidationResult
        {
            State = SessionState.Revoked,
            ChainId = SessionChainId.New(),
            RootId = SessionRootId.New(),

            Snapshot = new AuthStateSnapshot
            {
                Identity = new AuthIdentitySnapshot
                {
                    Tenant = Tenant,
                    UserKey = UserKey.Parse(
                        "user-1",
                        null)
                },
                Claims = ClaimsSnapshot.Empty
            }
        };

        var result =
            SessionValidationMapper.ToDomain(dto, AuthSessionId.Parse(SessionValue, null));

        result.IsValid.Should().BeFalse();
        result.State.Should().Be(SessionState.Revoked);
    }


    private static RemoteSessionValidator CreateSut(TestHttpMessageHandler handler, HttpContext? httpContext = null)
    {
        var httpClient = new HttpClient(handler)
        {
            BaseAddress =
                new Uri("https://uauth.test")
        };

        var accessor =
            new HttpContextAccessor
            {
                HttpContext =
                    httpContext ?? new DefaultHttpContext()
            };

        return new RemoteSessionValidator(
            httpClient,
            accessor);
    }

    private static SessionValidationContext CreateValidationContext()
    {
        return new SessionValidationContext
        {
            Tenant = Tenant,

            SessionId = AuthSessionId.Parse(SessionValue, null),

            Now = new DateTimeOffset(2026, 1, 1, 12, 0, 0, TimeSpan.Zero),

            Device = DeviceContext.Anonymous()
        };
    }

    private static HttpResponseMessage CreateFailureResponse(HttpStatusCode statusCode)
    {
        return new HttpResponseMessage(statusCode);
    }

    private sealed class TestHttpMessageHandler : HttpMessageHandler
    {
        public HttpResponseMessage Response { get; set; } = new(HttpStatusCode.OK);

        public Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? OnSendAsync { get; set; }

        public HttpMethod? Method { get; private set; }
        public Uri? RequestUri { get; private set; }
        public string? Body { get; private set; }
        public string? Cookie { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Method = request.Method;
            RequestUri = request.RequestUri;

            if (request.Content is not null)
            {
                Body = await request.Content.ReadAsStringAsync(
                    cancellationToken);
            }

            if (request.Headers.TryGetValues(
                    "Cookie",
                    out var cookieValues))
            {
                Cookie = cookieValues.SingleOrDefault();
            }

            if (OnSendAsync is not null)
            {
                return await OnSendAsync(
                    request,
                    cancellationToken);
            }

            return Response;
        }
    }
}
