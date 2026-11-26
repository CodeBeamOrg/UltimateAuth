using AwesomeAssertions;
using CodeBeam.UltimateAuth.Client;
using CodeBeam.UltimateAuth.Client.Errors;
using CodeBeam.UltimateAuth.Client.Events;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Tests.Unit.Client;

public sealed class UAuthLoginCompletionTests
{
    private static UAuthTransportResult Response(
        string? token = "Bearer jwt-value",
        string? format = "jwt",
        string? headerFormat = "bearer",
        string? expires = null,
        bool ok = true,
        int status = 200)
        => new()
        {
            Ok = ok,
            Status = status,
            AccessToken = token,
            AccessTokenFormat = format,
            AccessTokenHeaderFormat = headerFormat,
            AccessTokenExpiresAt = expires ??
                DateTimeOffset.UtcNow.AddMinutes(10).ToString("O")
        };

    private static AccessToken ExistingToken() => new()
    {
        Token = "existing-token",
        Format = TokenFormat.Jwt,
        ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15)
    };

    [Theory]
    [InlineData("jwt", "bearer", "Bearer abc", "abc", TokenFormat.Jwt)]
    [InlineData("opaque", "bearer", "bearer abc", "abc", TokenFormat.Opaque)]
    [InlineData("jwt", "raw", "abc", "abc", TokenFormat.Jwt)]
    public void ReadAccessToken_Parses_Supported_Formats(
        string format,
        string headerFormat,
        string value,
        string expected,
        TokenFormat expectedFormat)
    {
        var result = UAuthLoginCompletion.ReadAccessToken(
            Response(value, format, headerFormat));

        result.Should().NotBeNull();
        result!.Token.Should().Be(expected);
        result.Format.Should().Be(expectedFormat);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void ReadAccessToken_WithoutToken_ReturnsNull(string? token)
    {
        UAuthLoginCompletion.ReadAccessToken(
            Response(token)).Should().BeNull();
    }

    [Theory]
    [InlineData("unknown", "bearer", "Bearer abc")]
    [InlineData("jwt", "unknown", "abc")]
    [InlineData("jwt", "bearer", "abc")]
    [InlineData("jwt", "raw", "abc\rdef")]
    [InlineData("jwt", "raw", "abc\ndef")]
    public void ReadAccessToken_Rejects_InvalidMetadataOrValue(string format, string headerFormat, string value)
    {
        var act = () => UAuthLoginCompletion.ReadAccessToken(Response(value, format, headerFormat));
        act.Should().Throw<UAuthProtocolException>();
    }

    [Fact]
    public void ReadAccessToken_Rejects_InvalidExpiration()
    {
        var response = new UAuthTransportResult
        {
            Ok = true,
            Status = 200,
            AccessToken = "Bearer abc",
            AccessTokenFormat = "jwt",
            AccessTokenHeaderFormat = "bearer",
            AccessTokenExpiresAt = "not-a-date"
        };

        var act = () => UAuthLoginCompletion.ReadAccessToken(response);

        act.Should().Throw<UAuthProtocolException>();
    }

    [Fact]
    public async Task CompleteAsync_StoresToken_AndPublishesLogin()
    {
        var store = new InMemoryUAuthTokenStore();
        var events = new UAuthClientEvents();
        var published = new List<UAuthStateEvent>();

        events.StateChanged += args =>
        {
            published.Add(args.Type);
            return Task.CompletedTask;
        };

        await UAuthLoginCompletion.CompleteAsync(
            Response(),
            accessTokenRequired: true,
            store,
            events);

        store.Read()!.Token.Should().Be("jwt-value");
        store.Read()!.Format.Should().Be(TokenFormat.Jwt);

        published.Should().ContainSingle()
            .Which.Should().Be(UAuthStateEvent.LoginSucceeded);
    }

    [Fact]
    public async Task CompleteAsync_CookieOnlyLogin_ClearsPreviousToken()
    {
        var store = new InMemoryUAuthTokenStore();
        store.Replace(ExistingToken());

        var events = new UAuthClientEvents();

        await UAuthLoginCompletion.CompleteAsync(
            Response(token: null),
            accessTokenRequired: false,
            store,
            events);

        store.Read().Should().BeNull();
    }

    [Fact]
    public async Task CompleteAsync_MissingRequiredToken_FailsClosed()
    {
        var store = new InMemoryUAuthTokenStore();
        store.Replace(ExistingToken());

        var events = new UAuthClientEvents();
        var published = 0;

        events.StateChanged += _ =>
        {
            published++;
            return Task.CompletedTask;
        };

        var act = () => UAuthLoginCompletion.CompleteAsync(
            Response(token: null),
            accessTokenRequired: true,
            store,
            events);

        await act.Should().ThrowAsync<UAuthProtocolException>()
            .WithMessage("*required access token*");

        store.Read().Should().BeNull();
        published.Should().Be(0);
    }

    [Fact]
    public async Task CompleteAsync_ExpiredToken_FailsClosed()
    {
        var store = new InMemoryUAuthTokenStore();
        store.Replace(ExistingToken());

        var response = new UAuthTransportResult
        {
            Ok = true,
            Status = 200,
            AccessToken = "Bearer expired",
            AccessTokenFormat = "jwt",
            AccessTokenHeaderFormat = "bearer",
            AccessTokenExpiresAt =
                DateTimeOffset.UtcNow.AddMinutes(-5).ToString("O")
        };

        var act = () => UAuthLoginCompletion.CompleteAsync(
            response, true, store, new UAuthClientEvents());

        await act.Should().ThrowAsync<UAuthProtocolException>()
            .WithMessage("*expired access token*");

        store.Read().Should().BeNull();
    }

    [Theory]
    [InlineData(false, 200)]
    [InlineData(true, 401)]
    [InlineData(true, 500)]
    public async Task CompleteAsync_UnsuccessfulResponse_DoesNotMutateToken(
        bool ok, int status)
    {
        var store = new InMemoryUAuthTokenStore();
        store.Replace(ExistingToken());

        var act = () => UAuthLoginCompletion.CompleteAsync(
            Response(ok: ok, status: status),
            true,
            store,
            new UAuthClientEvents());

        await act.Should().ThrowAsync<UAuthProtocolException>();

        store.Read()!.Token.Should().Be("existing-token");
    }

    [Theory]
    [InlineData("""{"redirectUrl":"/home"}""", "/home")]
    [InlineData("""{"redirectUrl":null}""", null)]
    [InlineData("""{"other":"value"}""", null)]
    [InlineData("""{"redirectUrl":123}""", null)]
    public void GetRedirectUrl_HandlesResponseBody(
        string json,
        string? expected)
    {
        var response = new UAuthTransportResult
        {
            Ok = true,
            Status = 200,
            Body = System.Text.Json.JsonSerializer
                .Deserialize<System.Text.Json.JsonElement>(json)
        };

        UAuthLoginCompletion.GetRedirectUrl(response)
            .Should().Be(expected);
    }

    [Fact]
    public void GetRedirectUrl_WithoutBody_ReturnsNull()
    {
        UAuthLoginCompletion.GetRedirectUrl(
            new UAuthTransportResult())
            .Should().BeNull();
    }
}
