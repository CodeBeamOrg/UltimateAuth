---
title: Authorization
order: 4
group: plugin-domains
---

# Authorization & Policies

Authentication establishes an identity. Authorization determines what that identity is allowed to do.

UltimateAuth treats authorization as a separate responsibility, built around **structured actions, permissions, roles, and policies**.

Rather than relying exclusively on role names or scattered permission checks, UltimateAuth provides a consistent model for describing protected operations and evaluating access within their security context.

The Authorization Plugin Domain extends this model with permission management, role assignments, and supporting infrastructure.

## Understanding Authorization

A successful authentication does not automatically grant access to protected operations.

An authenticated identity may be permitted to read its own profile while being prohibited from modifying another user's account. An administrator may have elevated permissions within one tenant without having authority over another tenant.

These decisions require more than knowing whether the caller is authenticated.

UltimateAuth separates authorization into several responsibilities:

| Concept | Responsibility |
|---|---|
| **Action** | Identifies the protected operation |
| **Permission** | Represents authority to perform an action |
| **Role** | Groups permissions for assignment |
| **Policy** | Evaluates the conditions required to allow access |
| **AccessContext** | Carries the identity, target, tenant, and operation context |
| **AccessOrchestrator** | Coordinates policy evaluation and the access decision |

These concepts work together but are not interchangeable.

A permission can indicate that an identity is authorized to perform a class of operations. A policy may still deny a particular request because its target, tenant, or security context is invalid.

**Authorization is therefore a decision process, not simply a permission lookup.**

## Structured Actions

UltimateAuth identifies protected operations through a structured action model.

Actions use a hierarchical naming convention:

```text
resource.operation.scope
```

For operations involving a more specific resource:

```text
resource.subresource.operation.scope
```

Examples include:

```text
users.create.admin
users.delete.self
users.profile.update.self
credentials.change.self
sessions.revokechain.admin
```

The structure makes the intended operation and its access boundary explicit.

| Segment | Example | Meaning |
|---|---|---|
| Resource | `users` | The primary protected domain |
| Subresource | `profile` | An optional, more specific resource |
| Operation | `update` | The requested behavior |
| Scope | `self` | The intended access boundary |

This convention supports consistent naming, grouping, permission matching, and policy evaluation.

It also helps developers understand an operation without needing to inspect its endpoint implementation.

### UAuthActions

`UAuthActions` is UltimateAuth's built-in action catalog.

It provides predefined identifiers for framework operations so that services, endpoints, and authorization logic can refer to the same action consistently.

Examples of built-in actions include:

```text
flows.logout.self
sessions.listchains.admin
users.delete.self
credentials.revoke.admin
authorization.roles.assign.admin
```

Using a shared action catalog avoids duplicating string literals throughout the framework and reduces the risk of inconsistent permission names.

Actions are identifiers, not authorization decisions.

Referencing an action does not automatically authorize a request. The action must be evaluated against the applicable authorization rules.

Applications can also define actions for their own protected resources using the same naming convention.

## Understanding Scope

The final segment of an action describes its intended security scope.

UltimateAuth distinguishes three principal scopes:

| Scope | Meaning |
|---|---|
| `self` | An operation targeting the caller's own identity or resources |
| `admin` | An operation performed with elevated authority over another target |
| `anonymous` | An operation that can be initiated without an authenticated identity |

The scope is part of the action's identity.

### Self and Admin Are Different Permissions

Consider:

```text
users.delete.self
users.delete.admin
```

The first represents the authority to delete one's own account.

The second represents the authority to perform an administrative deletion.

Granting `users.delete.self` must not implicitly grant `users.delete.admin`.

Likewise, an authenticated user must not gain administrative access simply by supplying another user's identifier to a self-service operation.

The application must establish the actual relationship between the actor and the target.

**The requested scope is not proof that the caller satisfies that scope.**

A self operation requires a valid self relationship. An administrative operation requires the corresponding authority and must satisfy the applicable target and tenant policies.

### Anonymous Does Not Mean Unrestricted

An anonymous action is designed to be initiated without an authenticated session.

Examples include certain login and credential recovery operations.

However, anonymous availability does not remove the need for validation, proof requirements, abuse protection, or operation-specific security policies.

An anonymous credential reset request, for example, must not be allowed to replace a password without completing the required recovery proof.

## Permissions

A permission represents authority associated with a protected action.

In UltimateAuth, permission identifiers follow the same structured action convention.

For example:

```text
users.profile.update.self
```

A user granted this permission may be eligible to update their own profile, provided the other required policies succeed.

Permissions do not replace those policies.

A permission match answers whether the relevant authority is present. It does not independently establish that the target belongs to the caller, that the tenant boundary is valid, or that the account satisfies the required security conditions.

### Hierarchical Permissions

UltimateAuth supports hierarchical permission matching.

Examples:

| Permission | Intended coverage |
|---|---|
| `users.profile.update.self` | One specific action |
| `users.profile.*` | Actions under the profile group |
| `users.*` | Actions under the users resource |
| `*` | All actions covered by the permission matcher |

This allows permissions to be granted at different levels of granularity.

An application can assign a narrowly scoped permission to ordinary users while granting broader permission groups to trusted administrative roles.

### Permission Normalization

UltimateAuth provides permission normalization to reduce redundant representations.

When a permission set fully covers a known group of actions, the corresponding permissions can be represented by a wildcard for that group.

For example, complete coverage of a defined group may be normalized to:

```text
users.profile.*
```

Normalization is intended to preserve the effective permission coverage, not expand it.

A wildcard must not be introduced merely because several permissions share a prefix. The relevant group must actually be fully covered according to the known permission catalog.

### Compiled Permission Sets

Permission resolution may involve exact matches, hierarchical groups, and wildcards.

`CompiledPermissionSet` provides an optimized representation for evaluating these permissions at runtime.

This allows the authorization system to avoid repeatedly performing the same permission-processing work during access checks.

The optimization does not change the authorization model: permission matching remains one part of the overall access decision.

## Roles and Role Assignments

Roles provide a convenient way to manage collections of permissions.

Instead of assigning every permission directly to every user, an application can define roles corresponding to its operational responsibilities.

For example:

```text
SupportAgent
AccountManager
TenantAdministrator
```

Each role can contain the permissions required for that responsibility.

A role is not inherently a security bypass or a substitute for policy evaluation.

### Role Management

The reference authorization model supports role management and user-role assignments.

Its responsibilities include:

- Defining roles and their permissions.
- Assigning roles to users.
- Managing role membership.
- Resolving effective permissions.
- Maintaining the relevant tenant boundaries.

Roles and assignments are designed to support tenant-aware authorization.

An administrative role in one tenant must not automatically imply authority in another tenant.

### Roles Versus Permissions

Roles describe how authority is organized and assigned.

Permissions describe which operations that authority covers.

Policies determine whether the current operation is permitted under the relevant conditions.

Keeping these concepts separate allows applications to change role structures without redesigning their protected operations.

## Authorization Policies

Policies are the enforcement layer of UltimateAuth's authorization model.

A policy evaluates a security requirement against the current access context.

UltimateAuth includes policy concepts such as:

| Policy | Responsibility |
|---|---|
| `RequireAuthenticated` | Requires an authenticated actor |
| `RequireActiveUser` | Requires the relevant active-user condition |
| `RequireSelf` | Enforces the self-access relationship |
| `RequireSystem` | Restricts operations to an appropriate system context |
| `DenyCrossTenant` | Enforces tenant isolation |
| `MustHavePermission` | Requires the applicable permission |

These policies address different questions.

For example, `MustHavePermission` may confirm that the actor has `users.delete.admin`, while `DenyCrossTenant` may independently prevent the operation from targeting a user in an unauthorized tenant.

A successful permission match must not bypass another mandatory security policy.

### Policies Are Composable

A protected operation may require multiple conditions.

An administrative account operation might require:

1. An authenticated actor.
2. A valid and active identity.
3. Permission for the requested administrative action.
4. A valid target identity.
5. Compliance with tenant isolation rules.

Authorization succeeds only when the required conditions are satisfied.

This approach allows UltimateAuth to separate reusable security requirements from the business operation being protected.

> **Want to build your own authorization policies?** Follow [Define Authorization Policies](/docs/how-to-guides/define-authorization-policies) for a practical example of enforcing Separation of Duties, composing policies, and keeping authorization rules separate from application services.

## AccessContext and AccessOrchestrator

Authorization needs a trustworthy representation of the operation being evaluated.

`AccessContext` carries the relevant information for the decision, including the actor, target, tenant context, and requested operation.

The `AccessOrchestrator` coordinates the applicable policy evaluations and produces the authorization decision.

Conceptually, the process is:

```text
Authenticated identity
        |
        v
Requested action
        |
        v
Build AccessContext
        |
        v
Evaluate required policies
        |
        +---- Denied ---> Reject operation
        |
        v
       Allowed
        |
        v
Execute protected operation
```

The context must be constructed from trusted authentication state and validated operation data.

A caller-provided user ID, tenant ID, or scope string must not automatically be accepted as authoritative.

This is particularly important for administrative operations and multi-tenant applications.

### Authorization and Authentication Are Separate

`AuthFlowContext` and `AccessContext` serve different purposes.

`AuthFlowContext` belongs to authentication flow execution.

`AccessContext` belongs to authorization evaluation.

A successfully completed authentication flow may establish the identity required for an access context, but it does not replace authorization evaluation.

See [Flow-Based Authentication](/docs/fundamentals/flow-based-authentication) and [Request Lifecycle](/docs/fundamentals/request-lifecycle).

## Claims and Permission Resolution

UltimateAuth can integrate authorization information with authentication claims.

Relevant claims may include:

```text
ClaimTypes.Role
permission
tenant
```

Claims allow authorization information to participate in token-based and ASP.NET Core authorization scenarios.

However, claims are representations of authorization information, not necessarily its authoritative source.

### Session-Based Authorization

In session-authoritative authentication modes, authorization can rely on server-side state and the relevant runtime resolution mechanisms.

Changes to roles, permissions, or account security state can be reflected according to the session validation and authorization configuration.

### JWT-Based Authorization

In JWT-based modes, roles or permissions may be included in issued tokens.

A protected resource can evaluate those claims without contacting the authentication server on every request.

This introduces an important tradeoff.

**A permission removed after token issuance may remain represented in an already-issued JWT until that token expires.**

Applications requiring immediate permission revocation must account for this behavior through their token lifetimes, refresh strategy, or additional server-side validation.

The authorization model remains consistent, but the freshness of authorization information depends on how it is resolved.

## Protecting Application Endpoints

UltimateAuth's authorization architecture is not limited to its built-in identity management endpoints.

Applications can protect their own business resources using ASP.NET Core authorization and, where integrated, UltimateAuth's action and policy model.

### Requiring Authentication

For a controller-based resource API, standard ASP.NET Core authorization attributes can require an authenticated caller.

```csharp
[ApiController]
[Route("api/products")]
[Authorize]
public sealed class ProductsController : ControllerBase
{
    [HttpGet]
    public IActionResult GetProducts()
    {
        return Ok();
    }
}
```

When the application is configured with the appropriate UltimateAuth authentication integration, `[Authorize]` requires a valid authenticated identity.

This is the simplest protection boundary.

It does not, by itself, require a particular UltimateAuth action permission.

### Requiring a Named Policy

ASP.NET Core also supports policy-based endpoint authorization.

For example:

```csharp
[Authorize(Policy = "Products.Read")]
[HttpGet]
public IActionResult GetProducts()
{
    return Ok();
}
```

The named policy must be registered in the application's authorization configuration.

A standard ASP.NET Core policy can require a particular permission claim:

```csharp
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Products.Read", policy =>
    {
        policy.RequireAuthenticatedUser();
        policy.RequireClaim(
            "permission",
            "products.read.self");
    });
});
```

This example uses ASP.NET Core's built-in claim authorization.

It checks for the specified claim in the authenticated principal. It does not automatically execute UltimateAuth's `AccessOrchestrator` or its domain policies.

It is suitable when the application's security model permits authorization based on the available claims.

### Using UltimateAuth Actions for Application Resources

Applications can define their own action identifiers using the same convention as UltimateAuth's built-in catalog.

For example:

```csharp
public static class ProductActions
{
    public const string ReadSelf = "products.read.self";
    public const string CreateAdmin = "products.create.admin";
    public const string DeleteAdmin = "products.delete.admin";
}
```

These constants establish consistent names for application permissions.

They can be used by permission management, policy registration, and endpoint authorization integrations.

Defining the constants alone does not register them with the authorization system or enforce them at an endpoint.

For operations requiring actor-target validation, tenant isolation, or other UltimateAuth domain policies, the application must use the appropriate UltimateAuth authorization integration rather than relying solely on a permission claim.

The exact registration and invocation APIs depend on the configured authorization packages and hosting model.

## Authorization in Different Hosting Models

UltimateAuth supports different hosting architectures, and the enforcement boundary matters.

### Resource APIs

For HTTP resource APIs, authentication and authorization are normally enforced through the ASP.NET Core request pipeline.

Controllers and endpoints can use standard authorization attributes and policies.

The UltimateAuth resource API integration establishes the authentication context required for protected requests.

### Integrated Server Applications

In an integrated server application, business operations may execute directly through application services without making an HTTP request.

Such calls do not automatically pass through ASP.NET Core endpoint authorization attributes.

If an operation bypasses the HTTP endpoint, authorization must be enforced at an appropriate service or application boundary.

This is particularly relevant for Blazor Server applications that invoke services directly.

**Protecting an HTTP controller does not automatically protect every direct call to the underlying service.**

Authorization must remain effective at the actual execution boundary.

## Extending Authorization

Applications may need policies beyond the framework's built-in requirements.

Examples include:

- Resource ownership rules.
- Organization-specific access restrictions.
- Context-dependent administrative authority.
- Business workflow conditions.
- Additional tenant or resource boundaries.

UltimateAuth's policy-driven architecture is intended to support such requirements without embedding them directly into the authentication engine.

The reference Authorization domain provides a foundation for roles, permissions, and their management.

Applications can build on that foundation while keeping application-specific authorization rules close to the resources and operations they protect.

Detailed customization, custom policy implementation, and advanced permission registration are covered separately.

## Architectural Perspective

UltimateAuth's authorization model is built around a distinction:

**Actions describe operations. Permissions represent authority. Policies enforce security requirements.**

Roles make permissions manageable, while `AccessContext` and `AccessOrchestrator` provide the context and coordination needed for authorization decisions.

This allows applications to use consistent authorization semantics across authentication modes, hosting models, and business domains without reducing security to a collection of role checks.

## Next Steps

- [Users](/docs/plugin-domains/users) — Identity ownership, account lifecycle, and user management.
- [Credentials](/docs/plugin-domains/credentials) — Authentication evidence and credential security.
- [Plugin Domains](/docs/plugin-domains) — The broader Plugin Domain architecture.
- [Request Lifecycle](/docs/fundamentals/request-lifecycle) — How authentication and authorization participate in protected requests.