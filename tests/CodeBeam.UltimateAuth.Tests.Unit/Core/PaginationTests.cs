using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Options;
using CodeBeam.UltimateAuth.Users.Contracts;
using FluentAssertions;

namespace CodeBeam.UltimateAuth.Tests.Unit.Core;

public sealed class PaginationTests
{
    private readonly UAuthPaginationOptions _pagination = new()
    {
        DefaultPageSize = 25,
        MaxPageSize = 100
    };

    [Fact]
    public void Normalize_WhenPageNumberIsInvalid_UsesFirstPage()
    {
        var request = new PageRequest
        {
            PageNumber = -5,
            PageSize = 10
        };

        var result = request.Normalize(_pagination);

        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(10);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-100)]
    public void Normalize_WhenPageSizeIsInvalid_UsesConfiguredDefault(int pageSize)
    {
        var request = new PageRequest
        {
            PageSize = pageSize
        };

        var result = request.Normalize(_pagination);

        result.PageSize.Should().Be(25);
    }

    [Fact]
    public void Normalize_WhenPageSizeExceedsMaximum_ClampsToConfiguredMaximum()
    {
        var request = new PageRequest
        {
            PageSize = 500
        };

        var result = request.Normalize(_pagination);

        result.PageSize.Should().Be(100);
    }

    [Fact]
    public void Normalize_WhenPageSizeEqualsMaximum_PreservesValue()
    {
        var request = new PageRequest
        {
            PageSize = 100
        };

        var result = request.Normalize(_pagination);

        result.PageSize.Should().Be(100);
    }

    [Fact]
    public void Normalize_WhenValuesAreValid_PreservesValues()
    {
        var request = new PageRequest
        {
            PageNumber = 3,
            PageSize = 50,
            SortBy = "Name",
            Descending = true
        };

        var result = request.Normalize(_pagination);

        result.PageNumber.Should().Be(3);
        result.PageSize.Should().Be(50);
        result.SortBy.Should().Be("Name");
        result.Descending.Should().BeTrue();
    }

    [Fact]
    public void Normalize_DoesNotMutateOriginalRequest()
    {
        var request = new PageRequest
        {
            PageNumber = -1,
            PageSize = 500
        };

        var result = request.Normalize(_pagination);

        request.PageNumber.Should().Be(-1);
        request.PageSize.Should().Be(500);

        result.PageNumber.Should().Be(1);
        result.PageSize.Should().Be(100);

        result.Should().NotBeSameAs(request);
    }

    [Fact]
    public void Normalize_PreservesDerivedQueryType()
    {
        var request = new UserQuery
        {
            PageNumber = 0,
            PageSize = 500,
            Search = "admin"
        };

        var result = request.Normalize(_pagination);

        result.Should().BeOfType<UserQuery>();

        var normalized = (UserQuery)result;

        normalized.PageNumber.Should().Be(1);
        normalized.PageSize.Should().Be(100);
        normalized.Search.Should().Be("admin");
    }

    [Fact]
    public void Normalize_WhenPaginationPolicyIsNull_Throws()
    {
        var request = new PageRequest();

        var act = () => request.Normalize(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
