using CodeBeam.UltimateAuth.Core.Domain;
using FluentAssertions;
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
}
