using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Server.Auth;
using CodeBeam.UltimateAuth.Users;
using CodeBeam.UltimateAuth.Users.Contracts;
using FluentAssertions;
using Moq;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class AuthStateSnapshotFactoryTests
{
    [Fact]
    public async Task CreateAsync_should_return_snapshot_when_valid()
    {
        var provider = new Mock<IPrimaryUserIdentifierProvider>();
        var pprovider = new Mock<IUserProfileSnapshotProvider>();
        var lprovider = new Mock<IUserLifecycleSnapshotProvider>();

        provider.Setup(x => x.GetAsync(It.IsAny<TenantKey>(), It.IsAny<UserKey>(), default))
            .ReturnsAsync(new PrimaryUserIdentifiers
            {
                UserName = "admin"
            });

        var factory = new AuthStateSnapshotFactory(provider.Object, pprovider.Object, lprovider.Object);

        var validation = SessionValidationResult.Active(
            TenantKey.FromInternal("__single__"),
            UserKey.FromGuid(Guid.NewGuid()),
            default,
            default,
            default,
            ClaimsSnapshot.Empty,
            DateTimeOffset.UtcNow
        );

        var snapshot = await factory.CreateAsync(validation);

        snapshot.Should().NotBeNull();
        snapshot!.Identity.PrimaryUserName.Should().Be("admin");
    }

    [Fact]
    public async Task CreateAsync_should_return_null_when_invalid()
    {
        var provider = new Mock<IPrimaryUserIdentifierProvider>();
        var pprovider = new Mock<IUserProfileSnapshotProvider>();
        var lprovider = new Mock<IUserLifecycleSnapshotProvider>();

        var factory = new AuthStateSnapshotFactory(provider.Object, pprovider.Object, lprovider.Object);
        var validation = SessionValidationResult.Invalid(SessionState.NotFound);

        var snapshot = await factory.CreateAsync(validation);
        snapshot.Should().BeNull();
    }
}
