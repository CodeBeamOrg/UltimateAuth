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

    [Fact]
    public void Rotation_changes_generation_but_not_epoch()
    {
        var store = new InMemoryUAuthTokenStore();

        store.Replace(Token("old"));

        var before = store.Capture();

        Assert.True(store.TryRotate(before, Token("new")));

        var after = store.Capture();

        Assert.Equal(before.Epoch, after.Epoch);
        Assert.Equal(before.Generation + 1, after.Generation);
        Assert.Equal("new", after.Token!.Token);
    }

    [Fact]
    public void Logout_prevents_stale_refresh_from_restoring_token()
    {
        var store = new InMemoryUAuthTokenStore();

        store.Replace(Token("old"));

        var before = store.Capture();

        store.Clear();

        Assert.False(store.TryRotate(before, Token("rotated")));
        Assert.Null(store.Read());
    }

    [Fact]
    public void New_login_prevents_previous_refresh_from_replacing_token()
    {
        var store = new InMemoryUAuthTokenStore();

        store.Replace(Token("user-a"));

        var before = store.Capture();

        store.Replace(Token("user-b"));

        Assert.False(store.TryRotate(before, Token("user-a-rotated")));
        Assert.Equal("user-b", store.Read()!.Token);
    }

    [Fact]
    public void Stale_failure_cannot_clear_new_login()
    {
        var store = new InMemoryUAuthTokenStore();

        store.Replace(Token("user-a"));

        var before = store.Capture();

        store.Replace(Token("user-b"));

        Assert.False(store.TryClear(before));
        Assert.Equal("user-b", store.Read()!.Token);
    }

    [Fact]
    public void Same_snapshot_cannot_be_rotated_twice()
    {
        var store = new InMemoryUAuthTokenStore();

        store.Replace(Token("old"));

        var before = store.Capture();

        Assert.True(store.TryRotate(before, Token("first")));
        Assert.False(store.TryRotate(before, Token("second")));

        Assert.Equal("first", store.Read()!.Token);
    }

    private static AccessToken CreateToken(string value)
        => new()
        {
            Token = value,
            Format = TokenFormat.Jwt,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(5)
        };

    private static AccessToken Token(string value)
        => new()
        {
            Token = value,
            Format = TokenFormat.Jwt,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(10)
        };
}
