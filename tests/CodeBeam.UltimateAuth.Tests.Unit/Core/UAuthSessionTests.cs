using CodeBeam.UltimateAuth.Core.Domain;
using Xunit;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class UAuthSessionTests
{
    [Fact]
    public void Revoke_marks_session_as_revoked()
    {
        var now = DateTimeOffset.UtcNow;

        var session = UAuthSession<string>.Create(
            new AuthSessionId("s1"),
            tenantId: null,
            userId: "user-1",
            chainId: ChainId.New(),
            now,
            now.AddMinutes(10),
            DeviceInfo.Unknown,
            ClaimsSnapshot.Empty,
            SessionMetadata.Empty);

        var revoked = session.Revoke(now);

        Assert.False(session.IsRevoked);
        Assert.True(revoked.IsRevoked);
        Assert.Equal(now, revoked.RevokedAt);
    }

    [Fact]
    public void Revoking_twice_returns_same_instance()
    {
        var now = DateTimeOffset.UtcNow;

        var session = UAuthSession<string>.Create(
            new AuthSessionId("s1"),
            null,
            "user-1",
            ChainId.New(),
            now,
            now.AddMinutes(10),
            DeviceInfo.Unknown,
            ClaimsSnapshot.Empty,
            SessionMetadata.Empty);

        var revoked1 = session.Revoke(now);
        var revoked2 = revoked1.Revoke(now.AddMinutes(1));

        Assert.Same(revoked1, revoked2);
    }
}
