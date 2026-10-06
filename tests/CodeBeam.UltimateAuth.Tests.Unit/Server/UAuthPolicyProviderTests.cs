using CodeBeam.UltimateAuth.Server.Authorization;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

namespace CodeBeam.UltimateAuth.Tests.Unit.Server.Authorization;

public sealed class UAuthPolicyProviderTests
{
    [Fact]
    public async Task GetPolicyAsync_ShouldCreateUAuthActionRequirement()
    {
        var sut = Create();

        var policy =
            await sut.GetPolicyAsync("users.delete");

        policy.Should().NotBeNull();

        var requirement = policy!.Requirements
            .Should()
            .ContainSingle()
            .Subject
            .Should()
            .BeOfType<UAuthActionRequirement>()
            .Subject;

        requirement.Action.Should().Be("users.delete");
    }

    [Fact]
    public async Task GetPolicyAsync_ShouldPreservePolicyNameAsAction()
    {
        var sut = Create();

        var policy =
            await sut.GetPolicyAsync(
                "users.security.sessions.revoke");

        var requirement = policy!.Requirements
            .OfType<UAuthActionRequirement>()
            .Single();

        requirement.Action.Should()
            .Be("users.security.sessions.revoke");
    }

    [Fact]
    public async Task GetDefaultPolicyAsync_ShouldUseConfiguredDefaultPolicy()
    {
        var configured = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build();

        var options = new AuthorizationOptions
        {
            DefaultPolicy = configured
        };

        var sut = new UAuthPolicyProvider(
            Options.Create(options));

        var result = await sut.GetDefaultPolicyAsync();

        result.Should().BeSameAs(configured);
    }

    [Fact]
    public async Task GetFallbackPolicyAsync_ShouldUseConfiguredFallbackPolicy()
    {
        var configured = new AuthorizationPolicyBuilder()
            .RequireClaim("custom")
            .Build();

        var options = new AuthorizationOptions
        {
            FallbackPolicy = configured
        };

        var sut = new UAuthPolicyProvider(
            Options.Create(options));

        var result = await sut.GetFallbackPolicyAsync();

        result.Should().BeSameAs(configured);
    }

    private static UAuthPolicyProvider Create()
    {
        return new UAuthPolicyProvider(
            Options.Create(new AuthorizationOptions()));
    }
}