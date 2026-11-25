using CodeBeam.UltimateAuth.Client.Abstractions;
using CodeBeam.UltimateAuth.Client.Infrastructure;
using CodeBeam.UltimateAuth.Core.Contracts;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public sealed class UAuthTokenStoreTests
{
    [Fact]
    public void New_store_has_no_token()
    {
        var store = new InMemoryUAuthTokenStore();

        Assert.Null(store.Read());
    }

    [Fact]
    public void Replace_stores_access_token()
    {
        var store = new InMemoryUAuthTokenStore();
        var token = CreateToken("first");

        store.Replace(token);

        Assert.Same(token, store.Read());
    }

    [Fact]
    public void Replace_overwrites_previous_token()
    {
        var store = new InMemoryUAuthTokenStore();
        var first = CreateToken("first");
        var second = CreateToken("second");

        store.Replace(first);
        store.Replace(second);

        Assert.Same(second, store.Read());
    }

    [Fact]
    public void Clear_removes_token()
    {
        var store = new InMemoryUAuthTokenStore();

        store.Replace(CreateToken("first"));
        store.Clear();

        Assert.Null(store.Read());
    }

    [Fact]
    public void Stores_are_isolated()
    {
        var first = new InMemoryUAuthTokenStore();
        var second = new InMemoryUAuthTokenStore();

        first.Replace(CreateToken("first"));

        Assert.NotNull(first.Read());
        Assert.Null(second.Read());
    }

    [Fact]
    public void Expiration_uses_issuer_metadata()
    {
        var now = DateTimeOffset.UtcNow;
        var token = CreateToken("opaque-access-value");

        Assert.False(token.IsExpired(now));
        Assert.True(token.IsExpired(now.AddSeconds(601)));
    }

    private static AccessToken CreateToken(string value)
        => new()
        {
            Token = value,
            Format = TokenFormat.Jwt,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5)
        };
}
