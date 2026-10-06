using CodeBeam.UltimateAuth.Authorization.Policies;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Policies;
using CodeBeam.UltimateAuth.Policies.Registry;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public sealed class PolicyBuilderTests
{
    [Fact]
    public void For_ShouldRegisterPolicyForSpecifiedPrefix()
    {
        var (builder, registry, services) = CreateBuilder();

        builder
            .For("users.")
            .RequireAuthenticated();

        var compiled = registry.Build();

        var policies = compiled.Resolve(
            TestAccessContext.WithAction("users.get.self"),
            services);

        policies.Should().ContainSingle()
            .Which.Should().BeOfType<RequireAuthenticatedPolicy>();
    }

    [Fact]
    public void For_ShouldNotApplyPolicyToDifferentPrefix()
    {
        var (builder, registry, services) = CreateBuilder();

        builder
            .For("users.")
            .RequireAuthenticated();

        var compiled = registry.Build();

        var policies = compiled.Resolve(
            TestAccessContext.WithAction("sessions.get.self"),
            services);

        policies.Should().BeEmpty();
    }

    [Fact]
    public void Global_ShouldRegisterPolicyForEveryAction()
    {
        var (builder, registry, services) = CreateBuilder();

        builder
            .Global()
            .DenyCrossTenant();

        var compiled = registry.Build();

        var users = compiled.Resolve(
            TestAccessContext.WithAction("users.get.self"),
            services);

        var sessions = compiled.Resolve(
            TestAccessContext.WithAction("sessions.revoke.self"),
            services);

        users.Should().ContainSingle()
            .Which.Should().BeOfType<DenyCrossTenantPolicy>();

        sessions.Should().ContainSingle()
            .Which.Should().BeOfType<DenyCrossTenantPolicy>();
    }

    [Fact]
    public void ScopeBuilder_ShouldSupportFluentPolicyRegistration()
    {
        var (builder, registry, services) = CreateBuilder();

        builder
            .For("users.")
            .RequireAuthenticated()
            .RequireSelf()
            .RequirePermission()
            .DenyCrossTenant();

        var compiled = registry.Build();

        var selfPolicies = compiled.Resolve(
            TestAccessContext.WithAction("users.update.self"),
            services);

        selfPolicies.Should().Contain(x =>
            x is RequireAuthenticatedPolicy);

        selfPolicies.Should().Contain(x =>
            x is RequireSelfPolicy);

        selfPolicies.Should().Contain(x =>
            x is DenyCrossTenantPolicy);

        selfPolicies.Should().NotContain(x =>
            x is MustHavePermissionPolicy);

        var adminPolicies = compiled.Resolve(
            TestAccessContext.WithAction("users.update.admin"),
            services);

        adminPolicies.Should().Contain(x =>
            x is RequireAuthenticatedPolicy);

        adminPolicies.Should().Contain(x =>
            x is MustHavePermissionPolicy);

        adminPolicies.Should().Contain(x =>
            x is DenyCrossTenantPolicy);

        adminPolicies.Should().NotContain(x =>
            x is RequireSelfPolicy);
    }

    [Fact]
    public void RequireAuthenticated_ShouldRegisterCorrectPolicy()
    {
        AssertRegisteredPolicy<RequireAuthenticatedPolicy>(
            scope => scope.RequireAuthenticated());
    }

    [Fact]
    public void RequireSelf_ShouldRegisterCorrectPolicy()
    {
        AssertRegisteredPolicy<RequireSelfPolicy>(scope => scope.RequireSelf(), "users.test.self");
    }

    [Fact]
    public void RequirePermission_ShouldRegisterCorrectPolicy()
    {
        AssertRegisteredPolicy<MustHavePermissionPolicy>(scope => scope.RequirePermission(), "users.test.admin");
    }

    [Fact]
    public void DenyCrossTenant_ShouldRegisterCorrectPolicy()
    {
        AssertRegisteredPolicy<DenyCrossTenantPolicy>(
            scope => scope.DenyCrossTenant());
    }

    private static void AssertRegisteredPolicy<TPolicy>(
        Action<IPolicyScopeBuilder> configure)
        where TPolicy : IAccessPolicy
    {
        var (builder, registry, services) = CreateBuilder();

        configure(builder.For("users."));

        var compiled = registry.Build();

        var policies = compiled.Resolve(
            TestAccessContext.WithAction("users.test"),
            services);

        policies.Should().ContainSingle()
            .Which.Should().BeOfType<TPolicy>();
    }

    [Fact]
    public void Then_WhenConditionIsTrue_ShouldIncludePolicy()
    {
        var (scope, registry, services) = CreateScope();

        scope
            .When(_ => true)
            .Then()
            .RequireAuthenticated();

        var compiled = registry.Build();

        var policies = compiled.Resolve(
            TestAccessContext.WithAction("users.get.self"),
            services);

        policies.Should().ContainSingle();

        policies.Single()
            .Should()
            .BeOfType<ConditionalAccessPolicy>();
    }

    [Fact]
    public void Then_WhenConditionIsFalse_ShouldExcludePolicy()
    {
        var (scope, registry, services) = CreateScope();

        scope
            .When(_ => false)
            .Then()
            .RequireAuthenticated();

        var compiled = registry.Build();

        var policies = compiled.Resolve(
            TestAccessContext.WithAction("users.get.self"),
            services);

        policies.Should().BeEmpty();
    }

    [Fact]
    public void Otherwise_WhenConditionIsFalse_ShouldIncludePolicy()
    {
        var (scope, registry, services) = CreateScope();

        scope
            .When(_ => false)
            .Otherwise()
            .RequireAuthenticated();

        var compiled = registry.Build();

        var policies = compiled.Resolve(
            TestAccessContext.WithAction("users.get.self"),
            services);

        policies.Should().ContainSingle();

        policies.Single()
            .Should()
            .BeOfType<ConditionalAccessPolicy>();
    }

    [Fact]
    public void Otherwise_WhenConditionIsTrue_ShouldExcludePolicy()
    {
        var (scope, registry, services) = CreateScope();

        scope
            .When(_ => true)
            .Otherwise()
            .RequireAuthenticated();

        var compiled = registry.Build();

        var policies = compiled.Resolve(
            TestAccessContext.WithAction("users.get.self"),
            services);

        policies.Should().BeEmpty();
    }

    [Fact]
    public void ConditionalPolicy_ShouldReceiveRuntimeAccessContext()
    {
        var (scope, registry, services) = CreateScope();

        scope
            .When(context =>
                context.Action == "users.update.self")
            .Then()
            .RequireAuthenticated();

        var compiled = registry.Build();

        var matching = compiled.Resolve(
            TestAccessContext.WithAction("users.update.self"),
            services);

        var nonMatching = compiled.Resolve(
            TestAccessContext.WithAction("users.get.self"),
            services);

        matching.Should().ContainSingle();
        nonMatching.Should().BeEmpty();
    }

    [Fact]
    public void Then_ShouldSupportMultiplePolicies()
    {
        var (scope, registry, services) = CreateScope();

        scope
            .When(_ => true)
            .Then()
            .RequireAuthenticated()
            .RequireSelf()
            .RequirePermission()
            .DenyCrossTenant();

        var compiled = registry.Build();

        var policies = compiled.Resolve(
            TestAccessContext.WithAction("users.update.self"),
            services);

        policies.Should().HaveCount(4);

        policies.Should()
            .OnlyContain(x => x is ConditionalAccessPolicy);
    }

    [Fact]
    public void Then_ShouldPreserveActionPrefix()
    {
        var services = new ServiceCollection()
            .BuildServiceProvider();

        var registry = new AccessPolicyRegistry();

        var scope =
            new PolicyScopeBuilder(
                "users.",
                registry,
                services);

        scope
            .When(_ => true)
            .Then()
            .RequireAuthenticated();

        var compiled = registry.Build();

        var matching = compiled.Resolve(
            TestAccessContext.WithAction("users.get.self"),
            services);

        var differentPrefix = compiled.Resolve(
            TestAccessContext.WithAction("sessions.get.self"),
            services);

        matching.Should().ContainSingle();
        differentPrefix.Should().BeEmpty();
    }

    [Fact]
    public void For_WhenThen_ShouldBeAvailableThroughPublicBuilderContract()
    {
        var services = new ServiceCollection()
            .BuildServiceProvider();

        var registry = new AccessPolicyRegistry();

        IPolicyBuilder builder =
            new PolicyBuilder(registry, services);

        builder
            .For("users.")
            .When(_ => true)
            .Then()
            .RequireAuthenticated();

        var compiled = registry.Build();

        var policies = compiled.Resolve(
            TestAccessContext.WithAction("users.get.self"),
            services);

        policies.Should().ContainSingle();
    }

    private static void AssertRegisteredPolicy<TPolicy>(Action<IPolicyScopeBuilder> configure, string action) where TPolicy : IAccessPolicy
    {
        var (builder, registry, services) = CreateBuilder();

        configure(builder.For("users."));

        var compiled = registry.Build();
        var policies = compiled.Resolve(TestAccessContext.WithAction(action), services);

        policies.Should().ContainSingle().Which.Should().BeOfType<TPolicy>();
    }

    private static (PolicyScopeBuilder Scope, AccessPolicyRegistry Registry, ServiceProvider Services) CreateScope()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var registry = new AccessPolicyRegistry();

        return (
            new PolicyScopeBuilder("users.", registry, services),
            registry,
            services);
    }

    private static (PolicyBuilder Builder, AccessPolicyRegistry Registry, ServiceProvider Services) CreateBuilder()
    {
        var services = new ServiceCollection().BuildServiceProvider();
        var registry = new AccessPolicyRegistry();

        return (new PolicyBuilder(registry, services), registry, services);
    }
}
