using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public sealed class AuthSessionIdTests
{
    private const string ValidRaw = "session-aaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string AnotherValidRaw = "session-bbbbbbbbbbbbbbbbbbbbbbbbbbbb";

    [Fact]
    public void TryCreate_returns_false_for_null()
    {
        var result = AuthSessionId.TryCreate(null!, out var id);

        Assert.False(result);
        Assert.Equal(default, id);
    }

    [Fact]
    public void TryCreate_returns_false_for_empty_string()
    {
        var result = AuthSessionId.TryCreate(string.Empty, out var id);

        Assert.False(result);
        Assert.Equal(default, id);
    }

    [Fact]
    public void TryCreate_returns_false_for_short_value()
    {
        var result = AuthSessionId.TryCreate("too-short", out var id);

        Assert.False(result);
        Assert.Equal(default, id);
    }

    [Fact]
    public void TryCreate_creates_id_for_valid_value()
    {
        var result = AuthSessionId.TryCreate(ValidRaw, out var id);

        Assert.True(result);
        Assert.NotEqual(default, id);
        Assert.Equal(ValidRaw, id.Value);
    }

    [Fact]
    public void Equality_is_value_based()
    {
        AuthSessionId.TryCreate(ValidRaw, out var id1);
        AuthSessionId.TryCreate(ValidRaw, out var id2);

        Assert.Equal(id1, id2);
        Assert.True(id1 == id2);
        Assert.False(id1 != id2);
    }

    [Fact]
    public void Different_values_are_not_equal()
    {
        AuthSessionId.TryCreate(ValidRaw, out var id1);
        AuthSessionId.TryCreate(AnotherValidRaw, out var id2);

        Assert.NotEqual(id1, id2);
        Assert.True(id1 != id2);
    }

    [Fact]
    public void ToString_returns_raw_value()
    {
        AuthSessionId.TryCreate(ValidRaw, out var id);

        Assert.Equal(ValidRaw, id.ToString());
    }

    [Fact]
    public void Implicit_string_conversion_returns_raw_value()
    {
        AuthSessionId.TryCreate(ValidRaw, out var id);

        string value = id;

        Assert.Equal(ValidRaw, value);
    }

    [Fact]
    public void Default_value_has_null_Value()
    {
        var id = default(AuthSessionId);

        Assert.Null(id.Value);
    }
}
