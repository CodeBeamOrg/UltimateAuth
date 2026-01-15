using CodeBeam.UltimateAuth.Core.Domain;
using Xunit;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class UAuthSessionTests
{
    private const string ValidRaw = "session-aaaaaaaaaaaaaaaaaaaaaaaaaaaa";
    private const string ValidDeviceId = "deviceidshouldbelongandstrongenough!?1234567890";

    [Fact]
    public void Revoke_marks_session_as_revoked()
    {
        var now = DateTimeOffset.UtcNow;
        AuthSessionId.TryCreate(ValidRaw, out var sessionId);

        var session = UAuthSession.Create(
            sessionId: sessionId,
            tenantId: null,
            userKey: UserKey.FromString("user-1"),
            chainId: SessionChainId.New(),
            now,
            now.AddMinutes(10),
            new DeviceContext { DeviceId = DeviceId.Create(ValidDeviceId)},
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
        AuthSessionId.TryCreate(ValidRaw, out var sessionId);

        var session = UAuthSession.Create(
            sessionId,
            null,
            UserKey.FromString("user-1"),
            SessionChainId.New(),
            now,
            now.AddMinutes(10),
            new DeviceContext { DeviceId = DeviceId.Create(ValidDeviceId) },
            ClaimsSnapshot.Empty,
            SessionMetadata.Empty);

        var revoked1 = session.Revoke(now);
        var revoked2 = revoked1.Revoke(now.AddMinutes(1));

        Assert.Same(revoked1, revoked2);
    }
}
