using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Server.Options;
using CodeBeam.UltimateAuth.Users.Contracts;
using CodeBeam.UltimateAuth.Users.Reference;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Tests.Integration.EfCore.Users;

public sealed class UserQueryPaginationTests
{
    [Fact]
    public async Task QueryUsersAsync_AppliesConfiguredPaginationAndSorting()
    {
        await using var runtime = await EfCoreTestRuntime.CreateAsync(
            services =>
            {
                services.PostConfigure<UAuthServerOptions>(options =>
                {
                    options.Pagination.DefaultPageSize = 2;
                    options.Pagination.MaxPageSize = 3;
                });
            });

        var names = new[]
        {
            "Charlie",
            "Alice",
            "Echo",
            "Bob",
            "Delta"
        };

        var prefix = $"ef-pagination-{Guid.NewGuid():N}";

        //
        // Arrange: persist five users through the real service.
        //
        using (var scope = runtime.Services.CreateScope())
        {
            var service = scope.ServiceProvider
                .GetRequiredService<IUserApplicationService>();

            var createContext = TestAccessContext.ForUserCreation(
                TestUsers.Admin,
                UAuthActions.Users.CreateAnonymous);

            foreach (var name in names)
            {
                var result = await service.CreateUserAsync(
                    createContext,
                    new CreateUserRequest
                    {
                        UserName = $"{prefix}-{name}",
                        DisplayName = $"{prefix}-{name}"
                    });

                result.IsSuccess.Should().BeTrue();
            }
        }

        //
        // Act: use a fresh DI scope / DbContext.
        //
        using (var scope = runtime.Services.CreateScope())
        {
            var service = scope.ServiceProvider
                .GetRequiredService<IUserApplicationService>();

            // Use the action registered for querying users.
            var context = TestAccessContext.ForUserCreation(
                TestUsers.Admin,
                UAuthActions.Users.CreateAnonymous);

            var firstPage = await service.QueryUsersAsync(
                context,
                new UserQuery
                {
                    PageNumber = 1,
                    PageSize = 0,
                    Search = prefix,
                    SortBy = nameof(UserSummary.DisplayName)
                });

            firstPage.TotalCount.Should().Be(5);
            firstPage.PageNumber.Should().Be(1);
            firstPage.PageSize.Should().Be(2);

            firstPage.Items
                .Select(x => x.DisplayName)
                .Should()
                .Equal(
                    $"{prefix}-Alice",
                    $"{prefix}-Bob");

            //
            // MaxPageSize = 3.
            // Requested PageSize = 100 must be clamped.
            //
            var secondPage = await service.QueryUsersAsync(
                context,
                new UserQuery
                {
                    PageNumber = 2,
                    PageSize = 100,
                    Search = prefix,
                    SortBy = nameof(UserSummary.DisplayName)
                });

            secondPage.TotalCount.Should().Be(5);
            secondPage.PageNumber.Should().Be(2);
            secondPage.PageSize.Should().Be(3);

            secondPage.Items
                .Select(x => x.DisplayName)
                .Should()
                .Equal(
                    $"{prefix}-Delta",
                    $"{prefix}-Echo");
        }
    }

    [Fact]
    public async Task QueryUsersAsync_AcrossPages_ReturnsEveryUserExactlyOnce()
    {
        await using var runtime = await EfCoreTestRuntime.CreateAsync(
            services =>
            {
                services.PostConfigure<UAuthServerOptions>(options =>
                {
                    options.Pagination.DefaultPageSize = 2;
                    options.Pagination.MaxPageSize = 3;
                });
            });

        var names = new[]
            {
            "Charlie",
            "Alice",
            "Echo",
            "Bob",
            "Delta"
        };

        var prefix = $"ef-pages-{Guid.NewGuid():N}";

        // Arrange: create five users in non-alphabetical order.
        using (var scope = runtime.Services.CreateScope())
        {
            var service = scope.ServiceProvider
                .GetRequiredService<IUserApplicationService>();

            var context = TestAccessContext.ForUserCreation(
                TestUsers.Admin,
                UAuthActions.Users.CreateAnonymous);

            foreach (var name in names)
            {
                var result = await service.CreateUserAsync(
                    context,
                    new CreateUserRequest
                    {
                        UserName = $"{prefix}-{name}",
                        DisplayName = $"{prefix}-{name}"
                    });

                result.IsSuccess.Should().BeTrue();
            }
        }

        // Act: query all three pages using a fresh DbContext.
        using (var scope = runtime.Services.CreateScope())
        {
            var service = scope.ServiceProvider
                .GetRequiredService<IUserApplicationService>();

            var context = TestAccessContext.ForUserCreation(
                TestUsers.Admin,
                UAuthActions.Users.CreateAnonymous);

            var pages = new List<PagedResult<UserSummary>>();

            for (var pageNumber = 1; pageNumber <= 3; pageNumber++)
            {
                var result = await service.QueryUsersAsync(
                    context,
                    new UserQuery
                    {
                        PageNumber = pageNumber,
                        PageSize = 2,
                        Search = prefix,
                        SortBy = nameof(UserSummary.DisplayName)
                    });

                pages.Add(result);
            }

            // Assert: pagination metadata.
            pages.Should().HaveCount(3);

            pages.Select(x => x.TotalCount)
                .Should()
                .OnlyContain(x => x == 5);

            pages.Select(x => x.PageSize)
                .Should()
                .OnlyContain(x => x == 2);

            pages.Select(x => x.PageNumber)
                .Should()
                .Equal(1, 2, 3);

            pages.Select(x => x.Items.Count)
                .Should()
                .Equal(2, 2, 1);

            // Assert: deterministic ordering across pages.
            var allUsers = pages
                .SelectMany(x => x.Items)
                .ToList();

            allUsers.Select(x => x.DisplayName)
                .Should()
                .Equal(
                    $"{prefix}-Alice",
                    $"{prefix}-Bob",
                    $"{prefix}-Charlie",
                    $"{prefix}-Delta",
                    $"{prefix}-Echo");

            // Assert: no duplicate or missing users.
            allUsers.Should().HaveCount(5);

            allUsers.Select(x => x.UserKey)
                .Distinct()
                .Should()
                .HaveCount(5);
        }
    }
}
