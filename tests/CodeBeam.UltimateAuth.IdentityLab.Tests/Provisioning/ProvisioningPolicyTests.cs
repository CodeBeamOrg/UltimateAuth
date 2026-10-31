using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.IdentityLab.Policy;
using CodeBeam.UltimateAuth.IdentityLab.Services;

namespace CodeBeam.UltimateAuth.IdentityLab.Tests.Provisioning;

public sealed class ProvisioningPolicyTests(PostgresFixture postgres) : IClassFixture<PostgresFixture>
{
    [Theory]
    [InlineData(true)]
    [InlineData("trusted")]
    [InlineData("identitylab.internal.provisioning")]
    public async Task Client_values_cannot_forge_provisioning_or_rollback_tokens(object token)
    {
        await using var db = await postgres.CreateAsync();
        await db.SeedAsync();
        var before = await db.SnapshotAsync();
        var policy = new SandboxUAuthAccessPolicy(db, new LabRootResolver(), new());
        var attributes = new Dictionary<string, object>
        {
            ["identitylab.internal.provisioning"] = token,
            ["identitylab.internal.provisioning.rollback"] = token
        };
        foreach (var action in new[] { UAuthActions.Users.CreateAnonymous, UAuthActions.Users.CreateAdmin,
            UAuthActions.Users.DeleteAdmin })
        {
            var result = await policy.DecideAsync(Contexts.Create(action, LabDatabase.OwnerA,
                UserKey.FromString("unrelated"), attributes: attributes));
            Assert.True(result.IsDenied);
        }
        Assert.Equal(before, await db.SnapshotAsync());
    }
}
