using CodeBeam.UltimateAuth.Authorization.Contracts;
using CodeBeam.UltimateAuth.Authorization.Policies;
using CodeBeam.UltimateAuth.Core.Abstractions;
using CodeBeam.UltimateAuth.Core.Contracts;
using CodeBeam.UltimateAuth.Core.Defaults;
using CodeBeam.UltimateAuth.Core.Domain;
using CodeBeam.UltimateAuth.Core.MultiTenancy;
using CodeBeam.UltimateAuth.Policies;
using CodeBeam.UltimateAuth.Tests.Unit.Helpers;
using FluentAssertions;

namespace CodeBeam.UltimateAuth.Tests.Unit;

public sealed class AccessPoliciesTests
{

    public sealed class RequireAuthenticatedPolicyTests
    {
        [Fact]
        public void AppliesTo_NormalAction_ShouldReturnTrue()
        {
            var sut = new RequireAuthenticatedPolicy();

            var context = TestAccessContext.WithAction("users.get.self");

            sut.AppliesTo(context).Should().BeTrue();
        }

        [Fact]
        public void AppliesTo_AnonymousAction_ShouldReturnFalse()
        {
            var sut = new RequireAuthenticatedPolicy();

            var context = TestAccessContext.WithAction("users.create.anonymous");

            sut.AppliesTo(context).Should().BeFalse();
        }

        [Fact]
        public void Decide_UnauthenticatedActor_ShouldDeny()
        {
            var sut = new RequireAuthenticatedPolicy();

            var context = TestAccessContext.WithAction("users.get.self");

            var result = sut.Decide(context);

            result.IsAllowed.Should().BeFalse();
            result.DenyReason.Should().Be("unauthenticated");
        }

        [Fact]
        public void Decide_AuthenticatedActor_ShouldAllow()
        {
            var sut = new RequireAuthenticatedPolicy();

            var context = TestAccessContext.ForUser(
                UserKey.New(),
                "users.get.self");

            var result = sut.Decide(context);

            result.IsAllowed.Should().BeTrue();
        }
    }

    public sealed class DenyCrossTenantPolicyTests
    {
        [Fact]
        public void AppliesTo_ShouldAlwaysReturnTrue()
        {
            var sut = new DenyCrossTenantPolicy();

            sut.AppliesTo(
                TestAccessContext.WithAction("users.get.self"))
                .Should()
                .BeTrue();
        }

        [Fact]
        public void Decide_SameTenant_ShouldAllow()
        {
            var sut = new DenyCrossTenantPolicy();

            var context = TestAccessContext.ForUser(
                UserKey.New(),
                "users.get.self");

            var result = sut.Decide(context);

            result.IsAllowed.Should().BeTrue();
        }

        [Fact]
        public void Decide_CrossTenant_ShouldDeny()
        {
            var sut = new DenyCrossTenantPolicy();

            var actorTenant = TenantKey.FromExternal("tenant-a");
            var resourceTenant = TenantKey.FromExternal("tenant-b");

            var context = new AccessContext(
                actorUserKey: UserKey.New(),
                actorTenant: actorTenant,
                isAuthenticated: true,
                isSystemActor: false,
                actorChainId: null,
                resource: "users",
                targetUserKey: null,
                resourceTenant: resourceTenant,
                action: "users.get.admin",
                attributes: EmptyAttributes.Instance);

            var result = sut.Decide(context);

            result.IsAllowed.Should().BeFalse();
            result.DenyReason.Should().Be("cross_tenant_access_denied");
        }
    }

    public sealed class RequireSelfPolicyTests
    {
        [Fact]
        public void AppliesTo_SelfAction_ShouldReturnTrue()
        {
            var sut = new RequireSelfPolicy();

            var context =
                TestAccessContext.WithAction("users.update.self");

            sut.AppliesTo(context).Should().BeTrue();
        }

        [Theory]
        [InlineData("users.update.admin")]
        [InlineData("users.update.system")]
        [InlineData("users.update")]
        public void AppliesTo_NonSelfAction_ShouldReturnFalse(string action)
        {
            var sut = new RequireSelfPolicy();

            var context =
                TestAccessContext.WithAction(action);

            sut.AppliesTo(context).Should().BeFalse();
        }

        [Fact]
        public void Decide_UnauthenticatedActor_ShouldDeny()
        {
            var sut = new RequireSelfPolicy();

            var context =
                TestAccessContext.WithAction("users.update.self");

            var result = sut.Decide(context);

            result.IsAllowed.Should().BeFalse();
            result.DenyReason.Should().Be("unauthenticated");
        }

        [Fact]
        public void Decide_WhenActorIsTarget_ShouldAllow()
        {
            var sut = new RequireSelfPolicy();

            var userKey = UserKey.New();

            var context =
                TestAccessContext.ForUser(
                    userKey,
                    "users.update.self");

            var result = sut.Decide(context);

            result.IsAllowed.Should().BeTrue();
            result.DenyReason.Should().BeNull();
        }

        [Fact]
        public void Decide_WhenActorIsNotTarget_ShouldDeny()
        {
            var sut = new RequireSelfPolicy();

            var actor = UserKey.New();
            var target = UserKey.New();

            var context =
                TestAccessContext.ForTargetUser(
                    actor,
                    target,
                    "users.update.self");

            var result = sut.Decide(context);

            result.IsAllowed.Should().BeFalse();
            result.DenyReason.Should().Be("not_self");
        }
    }

    public sealed class RequireSystemPolicyTests
    {
        [Fact]
        public void AppliesTo_SystemAction_ShouldReturnTrue()
        {
            var sut = new RequireSystemPolicy();

            var context =
                TestAccessContext.WithAction("users.repair.system");

            sut.AppliesTo(context).Should().BeTrue();
        }

        [Theory]
        [InlineData("users.repair.admin")]
        [InlineData("users.repair.self")]
        [InlineData("users.repair")]
        public void AppliesTo_NonSystemAction_ShouldReturnFalse(string action)
        {
            var sut = new RequireSystemPolicy();

            var context =
                TestAccessContext.WithAction(action);

            sut.AppliesTo(context).Should().BeFalse();
        }

        [Fact]
        public void Decide_NormalActor_ShouldDeny()
        {
            var sut = new RequireSystemPolicy();

            var context =
                TestAccessContext.WithAction("users.repair.system");

            var result = sut.Decide(context);

            result.IsAllowed.Should().BeFalse();
            result.DenyReason.Should().Be("system_actor_required");
        }

        [Fact]
        public void Decide_SystemActor_ShouldAllow()
        {
            var sut = new RequireSystemPolicy();

            var context = new AccessContext(
                actorUserKey: null,
                actorTenant: TenantKey.System,
                isAuthenticated: false,
                isSystemActor: true,
                actorChainId: null,
                resource: "users",
                targetUserKey: null,
                resourceTenant: TenantKey.Single,
                action: "users.repair.system",
                attributes: EmptyAttributes.Instance);

            var result = sut.Decide(context);

            result.IsAllowed.Should().BeTrue();
            result.DenyReason.Should().BeNull();
        }

        [Fact]
        public void AppliesTo_SystemSuffixWithDifferentCasing_ShouldReturnFalse()
        {
            var sut = new RequireSystemPolicy();

            var context =
                TestAccessContext.WithAction("users.repair.SYSTEM");

            sut.AppliesTo(context).Should().BeFalse();
        }
    }

    public sealed class DenyAdminSelfModificationPolicyTests
    {
        [Fact]
        public void AppliesTo_AdminModificationWithTarget_ShouldReturnTrue()
        {
            var sut = new DenyAdminSelfModificationPolicy();

            var context =
                TestAccessContext.ForTargetUser(
                    UserKey.New(),
                    UserKey.New(),
                    "users.update.admin");

            sut.AppliesTo(context).Should().BeTrue();
        }

        [Theory]
        [InlineData("users.update.self")]
        [InlineData("users.update.system")]
        [InlineData("users.update")]
        public void AppliesTo_NonAdminAction_ShouldReturnFalse(string action)
        {
            var sut = new DenyAdminSelfModificationPolicy();

            var context =
                TestAccessContext.ForTargetUser(
                    UserKey.New(),
                    UserKey.New(),
                    action);

            sut.AppliesTo(context).Should().BeFalse();
        }

        [Fact]
        public void AppliesTo_AdminActionWithoutTarget_ShouldReturnFalse()
        {
            var sut = new DenyAdminSelfModificationPolicy();

            var context =
                TestAccessContext.WithAction("users.update.admin");

            sut.AppliesTo(context).Should().BeFalse();
        }

        [Theory]
        [InlineData("users.get.admin")]
        [InlineData("users.read.admin")]
        [InlineData("users.query.admin")]
        public void AppliesTo_AdminReadAction_ShouldReturnFalse(string action)
        {
            var sut = new DenyAdminSelfModificationPolicy();

            var context =
                TestAccessContext.ForTargetUser(
                    UserKey.New(),
                    UserKey.New(),
                    action);

            sut.AppliesTo(context).Should().BeFalse();
        }

        [Fact]
        public void Decide_UnauthenticatedActor_ShouldDeny()
        {
            var sut = new DenyAdminSelfModificationPolicy();

            var context =
                TestAccessContext.WithAction("users.update.admin");

            var result = sut.Decide(context);

            result.IsAllowed.Should().BeFalse();
            result.DenyReason.Should().Be("unauthenticated");
        }

        [Fact]
        public void Decide_AdminModifyingOwnAccount_ShouldDeny()
        {
            var sut = new DenyAdminSelfModificationPolicy();

            var userKey = UserKey.New();

            var context =
                TestAccessContext.ForTargetUser(
                    userKey,
                    userKey,
                    "users.update.admin");

            var result = sut.Decide(context);

            result.IsAllowed.Should().BeFalse();
            result.DenyReason.Should()
                .Be("admin_cannot_modify_own_account");
        }

        [Fact]
        public void Decide_AdminDeletingOwnAccount_ShouldDeny()
        {
            var sut = new DenyAdminSelfModificationPolicy();

            var userKey = UserKey.New();

            var context =
                TestAccessContext.ForTargetUser(
                    userKey,
                    userKey,
                    "users.delete.admin");

            var result = sut.Decide(context);

            result.IsAllowed.Should().BeFalse();
            result.DenyReason.Should()
                .Be("admin_cannot_modify_own_account");
        }

        [Fact]
        public void Decide_AdminModifyingDifferentUser_ShouldAllow()
        {
            var sut = new DenyAdminSelfModificationPolicy();

            var actor = UserKey.New();
            var target = UserKey.New();

            var context =
                TestAccessContext.ForTargetUser(
                    actor,
                    target,
                    "users.update.admin");

            var result = sut.Decide(context);

            result.IsAllowed.Should().BeTrue();
            result.DenyReason.Should().BeNull();
        }
    }

    public sealed class ConditionalAccessPolicyTests
    {
        [Fact]
        public void AppliesTo_WhenConditionMatchesExpectedTrue_ShouldReturnTrue()
        {
            var inner = new TestPolicy();

            var sut = new ConditionalAccessPolicy(
                _ => true,
                expected: true,
                inner);

            var context =
                TestAccessContext.WithAction("users.get.self");

            sut.AppliesTo(context).Should().BeTrue();
        }

        [Fact]
        public void AppliesTo_WhenConditionDoesNotMatchExpectedTrue_ShouldReturnFalse()
        {
            var inner = new TestPolicy();

            var sut = new ConditionalAccessPolicy(
                _ => false,
                expected: true,
                inner);

            var context =
                TestAccessContext.WithAction("users.get.self");

            sut.AppliesTo(context).Should().BeFalse();
        }

        [Fact]
        public void AppliesTo_WhenConditionMatchesExpectedFalse_ShouldReturnTrue()
        {
            var inner = new TestPolicy();

            var sut = new ConditionalAccessPolicy(
                _ => false,
                expected: false,
                inner);

            var context =
                TestAccessContext.WithAction("users.get.self");

            sut.AppliesTo(context).Should().BeTrue();
        }

        [Fact]
        public void AppliesTo_WhenConditionDoesNotMatchExpectedFalse_ShouldReturnFalse()
        {
            var inner = new TestPolicy();

            var sut = new ConditionalAccessPolicy(
                _ => true,
                expected: false,
                inner);

            var context =
                TestAccessContext.WithAction("users.get.self");

            sut.AppliesTo(context).Should().BeFalse();
        }

        [Fact]
        public void AppliesTo_ShouldPassContextToCondition()
        {
            AccessContext? receivedContext = null;

            var inner = new TestPolicy();

            var sut = new ConditionalAccessPolicy(
                context =>
                {
                    receivedContext = context;
                    return true;
                },
                expected: true,
                inner);

            var context =
                TestAccessContext.WithAction("users.get.self");

            sut.AppliesTo(context);

            receivedContext.Should().BeSameAs(context);
        }

        [Fact]
        public void Decide_ShouldDelegateToInnerPolicy()
        {
            var inner = new TestPolicy(
                AccessDecision.Deny("inner_denied"));

            var sut = new ConditionalAccessPolicy(
                _ => true,
                expected: true,
                inner);

            var context =
                TestAccessContext.WithAction("users.get.self");

            var result = sut.Decide(context);

            result.IsAllowed.Should().BeFalse();
            result.DenyReason.Should().Be("inner_denied");

            inner.DecideCallCount.Should().Be(1);
            inner.LastContext.Should().BeSameAs(context);
        }

        [Fact]
        public void Decide_ShouldReturnInnerAllowDecision()
        {
            var inner = new TestPolicy(
                AccessDecision.Allow());

            var sut = new ConditionalAccessPolicy(
                _ => true,
                expected: true,
                inner);

            var context =
                TestAccessContext.WithAction("users.get.self");

            var result = sut.Decide(context);

            result.IsAllowed.Should().BeTrue();
            inner.DecideCallCount.Should().Be(1);
        }

        private sealed class TestPolicy : IAccessPolicy
        {
            private readonly AccessDecision _decision;

            public int DecideCallCount { get; private set; }

            public AccessContext? LastContext { get; private set; }

            public TestPolicy()
                : this(AccessDecision.Allow())
            {
            }

            public TestPolicy(AccessDecision decision)
            {
                _decision = decision;
            }

            public bool AppliesTo(AccessContext context)
            {
                return true;
            }

            public AccessDecision Decide(AccessContext context)
            {
                DecideCallCount++;
                LastContext = context;

                return _decision;
            }
        }
    }

    public sealed class MustHavePermissionPolicyTests
    {
        [Fact]
        public void AppliesTo_AdminAction_ShouldReturnTrue()
        {
            var sut = new MustHavePermissionPolicy();

            var context =
                TestAccessContext.WithAction("users.update.admin");

            sut.AppliesTo(context).Should().BeTrue();
        }

        [Fact]
        public void AppliesTo_AdminActionWithDifferentCasing_ShouldReturnTrue()
        {
            var sut = new MustHavePermissionPolicy();

            var context =
                TestAccessContext.WithAction("users.update.ADMIN");

            sut.AppliesTo(context).Should().BeTrue();
        }

        [Theory]
        [InlineData("users.update.self")]
        [InlineData("users.update.system")]
        [InlineData("users.update.anonymous")]
        [InlineData("users.update")]
        public void AppliesTo_NonAdminAction_ShouldReturnFalse(string action)
        {
            var sut = new MustHavePermissionPolicy();

            var context =
                TestAccessContext.WithAction(action);

            sut.AppliesTo(context).Should().BeFalse();
        }

        [Fact]
        public void Decide_WhenPermissionsAttributeIsMissing_ShouldDeny()
        {
            var sut = new MustHavePermissionPolicy();

            var context =
                TestAccessContext.WithAction("users.update.admin");

            var result = sut.Decide(context);

            result.IsAllowed.Should().BeFalse();
            result.DenyReason.Should().Be("missing_permission");
        }

        [Fact]
        public void Decide_WhenPermissionsAttributeHasWrongType_ShouldDeny()
        {
            var sut = new MustHavePermissionPolicy();

            var context =
                TestAccessContext
                    .WithAction("users.update.admin")
                    .WithAttribute(
                        UAuthConstants.Access.Permissions,
                        "invalid-permissions");

            var result = sut.Decide(context);

            result.IsAllowed.Should().BeFalse();
            result.DenyReason.Should().Be("missing_permission");
        }
    }

    [Fact]
    public void Decide_WhenPermissionAllowsAction_ShouldAllow()
    {
        var sut = new MustHavePermissionPolicy();

        var permissions =
            CreatePermissions("users.update.admin");

        var context =
            TestAccessContext
                .WithAction("users.update.admin")
                .WithAttribute(
                    UAuthConstants.Access.Permissions,
                    permissions);

        var result = sut.Decide(context);

        result.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public void Decide_WhenPermissionDoesNotAllowAction_ShouldDeny()
    {
        var sut = new MustHavePermissionPolicy();

        var permissions =
            CreatePermissions("sessions.revoke.admin");

        var context =
            TestAccessContext
                .WithAction("users.update.admin")
                .WithAttribute(
                    UAuthConstants.Access.Permissions,
                    permissions);

        var result = sut.Decide(context);

        result.IsAllowed.Should().BeFalse();
        result.DenyReason.Should().Be("missing_permission");
    }

    [Fact]
    public void Decide_WhenPermissionAllowsExactAction_ShouldAllow()
    {
        var sut = new MustHavePermissionPolicy();

        var permissions =
            CreatePermissions("users.update.admin");

        var context =
            TestAccessContext
                .WithAction("users.update.admin")
                .WithAttribute(
                    UAuthConstants.Access.Permissions,
                    permissions);

        var result = sut.Decide(context);

        result.IsAllowed.Should().BeTrue();
        result.DenyReason.Should().BeNull();
    }

    public sealed class RequireActiveUserPolicyTests
    {
        [Fact]
        public void AppliesTo_AuthenticatedUser_ShouldReturnTrue()
        {
            var sut = CreatePolicy();

            var context = TestAccessContext.ForUser(
                UserKey.New(),
                "users.get.self");

            sut.AppliesTo(context).Should().BeTrue();
        }

        [Fact]
        public void AppliesTo_UnauthenticatedUser_ShouldReturnFalse()
        {
            var sut = CreatePolicy();

            var context =
                TestAccessContext.WithAction("users.get.self");

            sut.AppliesTo(context).Should().BeFalse();
        }

        [Fact]
        public void AppliesTo_AnonymousAction_ShouldReturnFalse()
        {
            var sut = CreatePolicy();

            var context = TestAccessContext.ForUser(
                UserKey.New(),
                "users.create.anonymous");

            sut.AppliesTo(context).Should().BeFalse();
        }

        [Fact]
        public void AppliesTo_AllowedInactiveAction_ShouldReturnFalse()
        {
            var sut = CreatePolicy();

            var context = TestAccessContext.ForUser(
                UserKey.New(),
                UAuthActions.Users.ChangeStatusSelf);

            sut.AppliesTo(context).Should().BeFalse();
        }

        [Fact]
        public void AppliesTo_SystemActor_ShouldReturnFalse()
        {
            var sut = CreatePolicy();

            var context = new AccessContext(
                actorUserKey: null,
                actorTenant: TenantKey.System,
                isAuthenticated: true,
                isSystemActor: true,
                actorChainId: null,
                resource: "users",
                targetUserKey: null,
                resourceTenant: TenantKey.System,
                action: "users.get.admin",
                attributes: EmptyAttributes.Instance);

            sut.AppliesTo(context).Should().BeFalse();
        }

        [Fact]
        public void Decide_WhenActorIsMissing_ShouldDeny()
        {
            var runtime = new TestUserRuntimeStateProvider();
            var sut = new RequireActiveUserPolicy(runtime);

            var context =
                TestAccessContext.WithAction("users.get.self");

            var result = sut.Decide(context);

            result.IsAllowed.Should().BeFalse();
            result.DenyReason.Should().Be("missing_actor");

            runtime.CallCount.Should().Be(0);
        }

        [Fact]
        public void Decide_WhenRuntimeStateIsMissing_ShouldDeny()
        {
            var runtime = new TestUserRuntimeStateProvider
            {
                Result = null
            };

            var sut = new RequireActiveUserPolicy(runtime);

            var context = TestAccessContext.ForUser(
                UserKey.New(),
                "users.get.self");

            var result = sut.Decide(context);

            result.IsAllowed.Should().BeFalse();
            result.DenyReason.Should().Be("user_not_found");
        }

        [Fact]
        public void Decide_WhenUserDoesNotExist_ShouldDeny()
        {
            var runtime = new TestUserRuntimeStateProvider
            {
                Result = CreateState(
                    exists: false,
                    isDeleted: false,
                    isActive: false)
            };

            var sut = new RequireActiveUserPolicy(runtime);

            var context = TestAccessContext.ForUser(
                UserKey.New(),
                "users.get.self");

            var result = sut.Decide(context);

            result.IsAllowed.Should().BeFalse();
            result.DenyReason.Should().Be("user_not_found");
        }

        [Fact]
        public void Decide_WhenUserIsDeleted_ShouldDeny()
        {
            var runtime = new TestUserRuntimeStateProvider
            {
                Result = CreateState(
                    exists: true,
                    isDeleted: true,
                    isActive: false)
            };

            var sut = new RequireActiveUserPolicy(runtime);

            var context = TestAccessContext.ForUser(
                UserKey.New(),
                "users.get.self");

            var result = sut.Decide(context);

            result.IsAllowed.Should().BeFalse();
            result.DenyReason.Should().Be("user_not_found");
        }

        [Fact]
        public void Decide_WhenUserIsInactive_ShouldDeny()
        {
            var runtime = new TestUserRuntimeStateProvider
            {
                Result = CreateState(
                    exists: true,
                    isDeleted: false,
                    isActive: false)
            };

            var sut = new RequireActiveUserPolicy(runtime);

            var context = TestAccessContext.ForUser(
                UserKey.New(),
                "users.get.self");

            var result = sut.Decide(context);

            result.IsAllowed.Should().BeFalse();
            result.DenyReason.Should().Be("user_not_active");
        }

        [Fact]
        public void Decide_WhenUserIsActive_ShouldAllow()
        {
            var runtime = new TestUserRuntimeStateProvider
            {
                Result = CreateState(
                    exists: true,
                    isDeleted: false,
                    isActive: true)
            };

            var sut = new RequireActiveUserPolicy(runtime);

            var context = TestAccessContext.ForUser(
                UserKey.New(),
                "users.get.self");

            var result = sut.Decide(context);

            result.IsAllowed.Should().BeTrue();
            result.DenyReason.Should().BeNull();
        }

        [Fact]
        public void Decide_ShouldQueryRuntimeUsingActorTenantAndUserKey()
        {
            var userKey = UserKey.New();
            var tenant = TenantKey.FromExternal("tenant-a");

            var runtime = new TestUserRuntimeStateProvider
            {
                Result = CreateState(
                    exists: true,
                    isDeleted: false,
                    isActive: true,
                    userKey: userKey)
            };

            var sut = new RequireActiveUserPolicy(runtime);
            var context = TestAccessContext.ForUser(userKey, "users.get.self", tenant);

            sut.Decide(context);

            runtime.LastTenant.Should().Be(tenant);
            runtime.LastUserKey.Should().Be(userKey);
            runtime.CallCount.Should().Be(1);
        }

        private static RequireActiveUserPolicy CreatePolicy()
        {
            return new RequireActiveUserPolicy(
                new TestUserRuntimeStateProvider());
        }

        private static UserRuntimeRecord CreateState(bool exists, bool isDeleted, bool isActive, UserKey? userKey = null)
        {
            return new UserRuntimeRecord
            {
                UserKey = userKey ?? UserKey.New(),
                Exists = exists,
                IsDeleted = isDeleted,
                IsActive = isActive,
                CanAuthenticate = isActive
            };
        }

        private sealed class TestUserRuntimeStateProvider : IUserRuntimeStateProvider
        {
            public UserRuntimeRecord? Result { get; init; }

            public int CallCount { get; private set; }

            public TenantKey? LastTenant { get; private set; }

            public UserKey? LastUserKey { get; private set; }

            public Task<UserRuntimeRecord?> GetAsync(
                TenantKey tenant,
                UserKey userKey,
                CancellationToken ct = default)
            {
                ct.ThrowIfCancellationRequested();

                CallCount++;

                LastTenant = tenant;
                LastUserKey = userKey;

                return Task.FromResult(Result);
            }
        }
    }

    private static CompiledPermissionSet CreatePermissions(
        params string[] permissions)
    {
        return new CompiledPermissionSet(
            permissions.Select(Permission.From));
    }
}
