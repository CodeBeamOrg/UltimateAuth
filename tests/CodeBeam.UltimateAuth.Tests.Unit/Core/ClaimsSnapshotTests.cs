using CodeBeam.UltimateAuth.Core.Domain;
using AwesomeAssertions;
using System.Security.Claims;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public class ClaimsSnapshotTests
{
    [Fact]
    public void ClaimsSnapshot_Should_Not_Change_When_Source_Is_Mutated()
    {
        var roles = new List<string> { "Admin" };

        var source =
            new Dictionary<string, IReadOnlyCollection<string>>
            {
                [ClaimTypes.Role] = roles
            };

        var snapshot = new ClaimsSnapshot(source);

        roles.Add("SuperAdmin");
        source[ClaimTypes.Role] = new List<string> { "Guest" };

        snapshot.IsInRole("Admin").Should().BeTrue();
        snapshot.IsInRole("SuperAdmin").Should().BeFalse();
        snapshot.IsInRole("Guest").Should().BeFalse();
    }

    [Fact]
    public void With_AddsClaimsWithoutMutatingOriginal()
    {
        var original = ClaimsSnapshot.From(
            (ClaimTypes.Role, "Admin"));

        var updated = original.With(
            (ClaimTypes.Role, "Editor"),
            ("uauth:permission", "users.read"));

        original.IsInRole("Editor").Should().BeFalse();
        updated.IsInRole("Admin").Should().BeTrue();
        updated.IsInRole("Editor").Should().BeTrue();
        updated.HasPermission("users.read").Should().BeTrue();

        updated.With().Should().BeSameAs(updated);
    }

    [Fact]
    public void Merge_CombinesClaimsWithoutDuplicates()
    {
        var first = ClaimsSnapshot.From(
            (ClaimTypes.Role, "Admin"));

        var second = ClaimsSnapshot.From(
            (ClaimTypes.Role, "Admin"),
            (ClaimTypes.Role, "Editor"),
            ("uauth:permission", "users.read"));

        var merged = first.Merge(second);

        merged.Roles.Should().BeEquivalentTo(
            new[] { "Admin", "Editor" });

        merged.HasPermission("users.read").Should().BeTrue();

        first.IsInRole("Editor").Should().BeFalse();
    }

    [Fact]
    public void Merge_EmptySnapshots_ReturnsExistingInstance()
    {
        var snapshot = ClaimsSnapshot.From(
            (ClaimTypes.Role, "Admin"));

        snapshot.Merge(ClaimsSnapshot.Empty)
            .Should().BeSameAs(snapshot);

        ClaimsSnapshot.Empty.Merge(snapshot)
            .Should().BeSameAs(snapshot);
    }

    [Fact]
    public void EqualSnapshots_HaveSameHashCodeRegardlessOfInsertionOrder()
    {
        var first = ClaimsSnapshot.From(
            (ClaimTypes.Role, "Admin"),
            (ClaimTypes.Role, "Editor"),
            ("uauth:permission", "users.read"));

        var second = ClaimsSnapshot.From(
            ("uauth:permission", "users.read"),
            (ClaimTypes.Role, "Editor"),
            (ClaimTypes.Role, "Admin"));

        first.Equals(second).Should().BeTrue();
        first.GetHashCode().Should().Be(second.GetHashCode());
    }
}
