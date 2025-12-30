using CodeBeam.UltimateAuth.Core.Domain;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class UAuthSessionChainTests
{
    [Fact]
    public void Rotating_chain_increments_rotation_count()
    {
        var chain = UAuthSessionChain<string>.Create(
            ChainId.New(),
            tenantId: null,
            userId: "user-1",
            securityVersion: 0,
            ClaimsSnapshot.Empty);

        var rotated = chain.RotateSession(new AuthSessionId("s2"));

        Assert.Equal(1, rotated.RotationCount);
        Assert.Equal("s2", rotated.ActiveSessionId?.Value);
    }

    [Fact]
    public void Revoked_chain_does_not_rotate()
    {
        var now = DateTimeOffset.UtcNow;

        var chain = UAuthSessionChain<string>.Create(
            ChainId.New(),
            null,
            "user-1",
            0,
            ClaimsSnapshot.Empty);

        var revoked = chain.Revoke(now);
        var rotated = revoked.RotateSession(new AuthSessionId("s2"));

        Assert.Same(revoked, rotated);
    }
}
