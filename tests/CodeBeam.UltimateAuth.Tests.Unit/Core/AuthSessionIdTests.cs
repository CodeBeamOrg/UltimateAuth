using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class AuthSessionIdTests
{
    [Fact]
    public void Cannot_create_empty_session_id()
    {
        Assert.Throws<ArgumentException>(() => new AuthSessionId(string.Empty));
    }

    [Fact]
    public void Equality_is_value_based()
    {
        var id1 = new AuthSessionId("abc");
        var id2 = new AuthSessionId("abc");

        Assert.Equal(id1, id2);
        Assert.True(id1 == id2);
    }
}
