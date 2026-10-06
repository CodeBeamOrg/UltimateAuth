using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Policies.Registry;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace CodeBeam.UltimateAuth.Tests.Unit.Policies;

public sealed class PolicyTests
{
    [Fact]
    public void Constructor_ShouldStoreActionPrefixAndFactory()
    {
        Func<AccessContext, IAccessPolicy> factory = _ => new TestPolicy();

        var rule = new PolicyRule("users.create", factory);

        rule.ActionPrefix.Should().Be("users.create");
        rule.Factory.Should().BeSameAs(factory);
    }

    [Theory]
    [InlineData("users.create")]
    [InlineData("users.create.admin")]
    [InlineData("USERS.CREATE.ADMIN")]
    public void Matches_WhenActionStartsWithPrefix_ShouldReturnTrue(string action)
    {
        var rule = new PolicyRule("users.create", _ => new TestPolicy());

        rule.Matches(action).Should().BeTrue();
    }

    [Theory]
    [InlineData("users.update")]
    [InlineData("sessions.create")]
    [InlineData("user.create")]
    public void Matches_WhenActionDoesNotStartWithPrefix_ShouldReturnFalse(string action)
    {
        var rule = new PolicyRule("users.create", _ => new TestPolicy());

        rule.Matches(action).Should().BeFalse();
    }

    [Fact]
    public void Build_WhenCalledTwice_ShouldThrow()
    {
        var registry = new AccessPolicyRegistry();

        registry.Build();

        var act = () => registry.Build();

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage(
                "AccessPolicyRegistry.Build() can only be called once.");
    }

    [Fact]
    public void Add_AfterBuild_ShouldThrow()
    {
        var registry = new AccessPolicyRegistry();

        registry.Build();

        var act = () =>
            registry.Add(
                "users.",
                _ => new TestPolicy());

        act.Should()
            .Throw<InvalidOperationException>()
            .WithMessage(
                "AccessPolicyRegistry is already built. Policies cannot be modified after Build().");
    }

    [Fact]
    public void Resolve_ShouldReturnPoliciesMatchingActionPrefix()
    {
        var registry = new AccessPolicyRegistry();

        registry.Add(
            "users.",
            _ => new TestPolicy("users"));

        registry.Add(
            "sessions.",
            _ => new TestPolicy("sessions"));

        var context = CreateContext("users.create");

        using var services =
            new ServiceCollection().BuildServiceProvider();

        var policies =
            registry.Resolve(context, services);

        policies.Should().ContainSingle();

        policies
            .Cast<TestPolicy>()
            .Single()
            .Name.Should()
            .Be("users");
    }

    [Fact]
    public void Resolve_ShouldMatchPrefixCaseInsensitively()
    {
        var registry = new AccessPolicyRegistry();

        registry.Add("USERS.", _ => new TestPolicy("users"));

        var context = CreateContext("users.create");

        using var services = new ServiceCollection().BuildServiceProvider();

        var policies = registry.Resolve(context, services);

        policies.Should().ContainSingle();
    }

    [Fact]
    public void Resolve_WhenNoPrefixMatches_ShouldReturnEmpty()
    {
        var registry = new AccessPolicyRegistry();

        registry.Add("sessions.", _ => new TestPolicy("sessions"));

        var context = CreateContext("users.create");

        using var services =
            new ServiceCollection().BuildServiceProvider();

        var policies =
            registry.Resolve(context, services);

        policies.Should().BeEmpty();
    }

    [Fact]
    public void CompiledSet_ShouldIncludePolicy_WhenPrefixMatchesAndPolicyApplies()
    {
        var registry = new AccessPolicyRegistry();

        registry.Add(
            "users.",
            _ => new TestPolicy(
                name: "matching",
                applies: true));

        var compiled = registry.Build();

        var context = CreateContext("users.create");

        using var services =
            new ServiceCollection().BuildServiceProvider();

        var policies =
            compiled.Resolve(context, services);

        policies.Should().ContainSingle();
    }

    [Fact]
    public void CompiledSet_ShouldExcludePolicy_WhenPolicyDoesNotApply()
    {
        var registry = new AccessPolicyRegistry();

        registry.Add(
            "users.",
            _ => new TestPolicy(
                name: "not-applicable",
                applies: false));

        var compiled = registry.Build();

        var context = CreateContext("users.create");

        using var services =
            new ServiceCollection().BuildServiceProvider();

        var policies =
            compiled.Resolve(context, services);

        policies.Should().BeEmpty();
    }

    [Fact]
    public void CompiledSet_ShouldNotCreatePolicy_WhenPrefixDoesNotMatch()
    {
        var registry = new AccessPolicyRegistry();

        var factoryCalled = false;

        registry.Add(
            "sessions.",
            _ =>
            {
                factoryCalled = true;
                return new TestPolicy("sessions", true);
            });

        var compiled = registry.Build();

        var context = CreateContext("users.create");

        using var services =
            new ServiceCollection().BuildServiceProvider();

        var policies =
            compiled.Resolve(context, services);

        policies.Should().BeEmpty();
        factoryCalled.Should().BeFalse();
    }

    [Fact]
    public void CompiledSet_ShouldProvideServiceProviderToPolicyFactory()
    {
        var dependency = new TestDependency();

        var services = new ServiceCollection()
            .AddSingleton(dependency)
            .BuildServiceProvider();

        var registry = new AccessPolicyRegistry();

        registry.Add(
            "users.",
            sp => new DependencyPolicy(
                sp.GetRequiredService<TestDependency>()));

        var compiled = registry.Build();

        var policies =
            compiled.Resolve(
                CreateContext("users.create"),
                services);

        var policy =
            policies.Should()
                .ContainSingle()
                .Subject
                .Should()
                .BeOfType<DependencyPolicy>()
                .Subject;

        policy.Dependency.Should().BeSameAs(dependency);
    }

    [Fact]
    public void Build_ShouldOrderPoliciesByPrefixLength()
    {
        var registry = new AccessPolicyRegistry();

        registry.Add(
            "users.create.",
            _ => new TestPolicy("specific"));

        registry.Add(
            "",
            _ => new TestPolicy("global"));

        registry.Add(
            "users.",
            _ => new TestPolicy("users"));

        var compiled = registry.Build();

        using var services =
            new ServiceCollection().BuildServiceProvider();

        var policies =
            compiled.Resolve(
                CreateContext("users.create.admin"),
                services);

        policies
            .Cast<TestPolicy>()
            .Select(x => x.Name)
            .Should()
            .ContainInOrder(
                "global",
                "users",
                "specific");
    }

    private sealed class TestPolicy : IAccessPolicy
    {
        private readonly bool _applies;

        public string? Name { get; }

        public TestPolicy(string? name = null, bool applies = true)
        {
            Name = name;
            _applies = applies;
        }

        public bool AppliesTo(AccessContext context) => _applies;

        public AccessDecision Decide(AccessContext context) => AccessDecision.Allow();
    }

    private sealed class TestDependency
    {
    }

    private sealed class DependencyPolicy : IAccessPolicy
    {
        public TestDependency Dependency { get; }

        public DependencyPolicy(TestDependency dependency)
        {
            Dependency = dependency;
        }

        public bool AppliesTo(AccessContext context)
            => true;

        public AccessDecision Decide(AccessContext context)
            => AccessDecision.Allow();
    }

    private static AccessContext CreateContext(string action)
    {
        return TestAccessContext.WithAction(action);
    }
}
