using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public sealed class UAuthSessionChainTests
{
    private static AuthSessionId CreateSessionId(string seed)
    {
        var raw = seed.PadRight(32, 'x');
        AuthSessionId.TryCreate(raw, out var id);
        return id;
    }

    [Fact]
    public void New_chain_has_expected_initial_state()
    {
        var chain = UAuthSessionChain.Create(
            SessionChainId.New(),
            SessionRootId.New(),
            tenantId: null,
            userKey: UserKey.FromString("user-1"),
            securityVersion: 0,
            ClaimsSnapshot.Empty);

        Assert.Equal(0, chain.RotationCount);
        Assert.Null(chain.ActiveSessionId);
        Assert.False(chain.IsRevoked);
    }

    [Fact]
    public void Rotating_chain_sets_active_session_and_increments_rotation()
    {
        var chain = UAuthSessionChain.Create(
            SessionChainId.New(),
            SessionRootId.New(),
            null,
            UserKey.FromString("user-1"),
            0,
            ClaimsSnapshot.Empty);

        var sessionId = CreateSessionId("s1");
        var rotated = chain.RotateSession(sessionId);

        Assert.Equal(1, rotated.RotationCount);
        Assert.Equal(sessionId, rotated.ActiveSessionId);
        Assert.NotSame(chain, rotated);
    }

    [Fact]
    public void Multiple_rotations_increment_rotation_count()
    {
        var chain = UAuthSessionChain.Create(
            SessionChainId.New(),
            SessionRootId.New(),
            null,
            UserKey.FromString("user-1"),
            0,
            ClaimsSnapshot.Empty);

        var first = chain.RotateSession(CreateSessionId("s1"));
        var second = first.RotateSession(CreateSessionId("s2"));

        Assert.Equal(2, second.RotationCount);
        Assert.Equal(CreateSessionId("s2"), second.ActiveSessionId);
    }

    [Fact]
    public void Revoked_chain_does_not_rotate()
    {
        var now = DateTimeOffset.UtcNow;

        var chain = UAuthSessionChain.Create(
            SessionChainId.New(),
            SessionRootId.New(),
            null,
            UserKey.FromString("user-1"),
            0,
            ClaimsSnapshot.Empty);

        var revoked = chain.Revoke(now);
        var rotated = revoked.RotateSession(CreateSessionId("s2"));

        Assert.Same(revoked, rotated);
        Assert.True(rotated.IsRevoked);
    }

    [Fact]
    public void Revoking_chain_sets_revocation_fields()
    {
        var now = DateTimeOffset.UtcNow;

        var chain = UAuthSessionChain.Create(
            SessionChainId.New(),
            SessionRootId.New(),
            null,
            UserKey.FromString("user-1"),
            0,
            ClaimsSnapshot.Empty);

        var revoked = chain.Revoke(now);

        Assert.True(revoked.IsRevoked);
        Assert.Equal(now, revoked.RevokedAt);
    }

    [Fact]
    public void Revoking_already_revoked_chain_is_idempotent()
    {
        var now = DateTimeOffset.UtcNow;

        var chain = UAuthSessionChain.Create(
            SessionChainId.New(),
            SessionRootId.New(),
            null,
            UserKey.FromString("user-1"),
            0,
            ClaimsSnapshot.Empty);

        var revoked1 = chain.Revoke(now);
        var revoked2 = revoked1.Revoke(now.AddMinutes(1));

        Assert.Same(revoked1, revoked2);
    }
}
