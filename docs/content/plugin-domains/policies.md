---
title: Policies & Access Control
order: 5
group: plugin-domains
---

# Policies & Access Control

Authorization is not simply a question of whether an identity possesses a permission.

A protected operation may involve an authenticated actor, a target identity, tenant boundaries, resource ownership, and security conditions that cannot be represented by a permission alone.

UltimateAuth addresses these requirements through a **policy-driven access control pipeline**.

Policies are independent decision units evaluated within a shared access context. The authorization orchestrator coordinates those decisions and determines whether the requested operation can proceed.

This page explains how that process works.

For actions, permissions, roles, and scope conventions, see [Authorization](/docs/plugin-domains/authorization).

## The Access Decision Model

UltimateAuth evaluates authorization using three primary elements:

| Element | Responsibility |
|---|---|
| **AccessContext** | Describes the actor, target, operation, and relevant security context |
| **Policies** | Evaluate individual authorization requirements |
| **AccessOrchestrator** | Coordinates policy resolution, evaluation, and execution |

These elements separate the description of an operation from the rules used to authorize it.

A permission may grant authority to perform an action, but policies determine whether that authority is valid in the current context.

For example, a caller may possess an administrative permission while attempting to access a resource belonging to another tenant.

The permission may match, but the operation must still satisfy tenant isolation requirements.

## AccessContext

`AccessContext` represents the security context of an authorization operation.

It provides the information needed to evaluate access without requiring each policy to reconstruct the request independently.

Conceptually, the context includes:

| Component | Purpose |
|---|---|
| **Actor** | The identity or system principal performing the operation |
| **Target** | The identity or resource affected by the operation |
| **Action** | The protected operation being requested |
| **Tenant context** | The tenant boundaries relevant to the actor and target |
| **Authorization information** | Relevant claims, permissions, and security state |

The context is more than a collection of request parameters.

It establishes the relationship between the authenticated actor and the operation being performed.

### Actor and Target Are Different Concepts

Consider an administrative operation:

```text
users.delete.admin
```

The actor is the administrator requesting the deletion.

The target is the user account being deleted.

These identities must remain distinguishable throughout authorization and execution.

For a self-service operation:

```text
users.delete.self
```

The actor and target are expected to represent the same identity.

That relationship must be established from trusted context, not assumed from the action suffix.

### Multi-Target Authorization

Some operations affect or retrieve multiple users within a single request.

For example, an application may need to retrieve the summaries of several users belonging to a team.

A traditional single-target authorization context identifies one target user:

```csharp
context.TargetUserKey
```

Batch operations may not have a single target.

UltimateAuth therefore also provides:

```csharp
context.TargetUserKeys
```

This property contains the user keys associated with a multi-target operation.

The collection is normalized, immutable, and available to authorization policies.

| Property | Purpose |
|---|---|
| `TargetUserKey` | Identifies a single target user |
| `TargetUserKeys` | Identifies multiple target users |
| `HasTarget` | Indicates whether a single target is present |
| `HasMultipleTargets` | Indicates whether a batch target collection is present |

Single-target and multi-target operations remain distinct.

An operation targeting multiple users does not automatically become a self-service operation, even when the actor is included in the requested collection.

### Creating a Multi-Target Context

Server-side operations can construct a multi-target authorization context through `IAccessContextFactory`:

```csharp
var accessContext =
    await accessContextFactory.CreateForUserTargetsAsync(
        authFlow: authFlow,
        action: UAuthActions.Users.GetByKeysAdmin,
        resource: "users",
        targetUserKeys: userKeys,
        ct: cancellationToken);
```

The factory preserves the existing actor, tenant, role resolution, and authorization context construction mechanisms.

The requested target collection is supplied explicitly.

The resulting context can be evaluated by the existing authorization pipeline without introducing a separate authorization mechanism.

### Example: Restrict Batch Access to Group Members

Consider an application where users belong to groups.

A member may retrieve the summaries of other users in the same group, but must not retrieve users belonging to another group.

The application can implement this restriction through an `IAccessPolicy`.

The following example illustrates the target validation logic inside a policy:

```csharp
if (context.Action == UAuthActions.Users.GetByKeysAdmin)
{
    var requestedKeys = context.TargetUserKeys;

    if (requestedKeys.Count == 0)
    {
        return AccessDecision.Deny("batch_targets_required");
    }

    var keys = requestedKeys.Distinct().ToArray();

    var allowedCount = await db.GroupMembers
        .AsNoTracking()
        .CountAsync(
            member =>
                member.GroupId == actorGroupId &&
                keys.Contains(member.UserKey),
            cancellationToken);

    if (allowedCount != keys.Length)
    {
        return AccessDecision.Deny("batch_target_out_of_scope");
    }

    return AccessDecision.Allow();
}
```

Here, `db`, `actorGroupId`, and `cancellationToken` represent application-specific dependencies and values resolved by the surrounding policy.

The example assumes that each user has at most one membership record within the selected group.

The policy verifies that every requested user belongs to the actor's authorized group.

If any target falls outside the permitted scope, the entire operation is denied.

This prevents a request from mixing authorized and unauthorized users to obtain information about another group.

### Authorization Must Cover the Complete Batch

A secure multi-target policy should evaluate every requested target.

Checking only the first user or allowing the operation because at least one user is authorized is insufficient.

For example:

```text
Requested users:
  User A — Group 1
  User B — Group 1
  User C — Group 2

Actor:
  Member of Group 1

Decision:
  Deny
```

The entire request is denied because User C is outside the actor's permitted scope.

Applications should not silently return unauthorized users or assume that a batch operation is safe merely because its action permission was granted.

### Permission and Scope Are Separate Requirements

The batch user retrieval action is:

```text
users.getbykeys.admin
```

An application may grant this permission to ordinary users when group-level access is required.

However, permission evaluation and target scope validation solve different problems.

- **Permission:** May this actor perform batch user retrieval?
- **Scope policy:** May this actor retrieve these particular users?

Both requirements must be satisfied.

An application's authorization policy must enforce the relevant target restrictions.

### Trusted Target Collections

Target keys originate from an operation request, but they are not automatically trusted authorization facts.

The server must validate and normalize the requested keys before constructing the authorization context.

The same normalized collection must be used for authorization and execution.

Otherwise, a policy could approve one collection while the application service retrieves another.

Applications should also ensure that batch authorization fails closed when required target information is missing.

`TargetUserKeys` makes multi-target authorization possible; it does not automatically enforce group membership, ownership, or other application-specific restrictions.

### Trusted Context Construction

Authorization decisions are only as reliable as the information supplied to them.

An endpoint may receive a target user ID or tenant ID from the caller, but these values must not automatically become trusted authorization facts.

The authentication infrastructure establishes the actor's identity. The application or domain operation resolves the intended target. The authorization context brings those elements together.

Policies then evaluate the resulting relationships.

This distinction is essential for preventing cross-user and cross-tenant access.

## Policies as Decision Units

A policy evaluates a specific security requirement.

Rather than combining every authorization rule into one large conditional expression, UltimateAuth separates requirements into reusable policies.

Examples include:

- Requiring an authenticated actor.
- Requiring an active user.
- Enforcing tenant isolation.
- Verifying self-access relationships.
- Requiring an appropriate permission.
- Restricting an operation to a system context.

Each policy evaluates its own responsibility within the available access context.

A policy should not need to understand every other policy to make its decision.

This allows authorization rules to be composed without losing their individual meaning.

## Policy Resolution

Not every policy is relevant to every operation.

UltimateAuth supports policy resolution based on action matching and runtime applicability.

### Action-Based Matching

Policies can be associated with action patterns.

For example:

```text
users.create.admin
users.*
authorization.roles.*
```

An action-specific policy can target one operation, while a broader pattern can apply to a related group of operations.

This allows common rules to be shared across a domain without duplicating policy registration for every action.

### Runtime Applicability

Action matching alone may not provide enough information to determine whether a policy should execute.

A policy can also evaluate whether it applies to the current context through `AppliesTo`.

This enables context-dependent policy selection.

For example, a policy may apply only to administrative operations that modify account state, rather than every administrative read operation.

**Policy resolution determines which rules participate in the decision. Policy evaluation determines whether those rules permit the operation.**

Keeping these stages separate makes authorization behavior easier to reason about.

## Policy Categories

UltimateAuth distinguishes between security requirements that must be enforced broadly and policies selected for particular operations.

### Invariants

Invariants represent fundamental security rules that must not be bypassed by ordinary permission grants or application-level authorization decisions.

They are evaluated before the remaining policy stages.

An invariant is not equivalent to a role permission or an optional business rule.

Its purpose is to preserve a security condition that must hold regardless of the requested operation's ordinary authorization configuration.

### Global Policies

Global policies apply broadly across protected operations.

They provide common security requirements that should not need to be registered independently for every action.

Examples of such requirements include authentication and tenant isolation.

Their applicability may still depend on the operation's context. An intentionally anonymous action, for example, cannot be treated identically to an authenticated administrative operation.

### Runtime Policies

Runtime policies are selected and evaluated according to the requested action and current access context.

They support requirements such as:

- Permission checks.
- Self-access validation.
- Active-user requirements.
- Operation-specific restrictions.

This separation allows the authorization system to maintain mandatory security boundaries while remaining extensible for individual domains and applications.

## Policy Evaluation Order

UltimateAuth's authorization pipeline follows a defined evaluation order:

1. **Invariants**
2. **Global policies**
3. **Runtime policies**

This order establishes a clear security hierarchy.

A runtime permission grant must not override an invariant that has already rejected the operation.

Likewise, satisfying an action-specific policy does not eliminate the need to satisfy applicable global requirements.

### Deny Takes Precedence

The decision model is deny-oriented.

When a required policy denies an operation, a later policy cannot convert that denial into an authorization grant.

This prevents authorization from depending on the accidental presence of a permissive policy.

### Allow Is Not a Universal Grant

A policy returning an allow result means that its own requirement has been satisfied or that it has no objection to the operation.

It does not mean that all other requirements have been satisfied.

The overall decision is produced by the authorization pipeline, not by one successful policy in isolation.

### Reauthentication

Some authorization decisions may require additional authentication rather than producing an ordinary allow or deny outcome.

UltimateAuth's decision model includes the ability to represent a reauthentication requirement.

This allows authorization to distinguish between insufficient authority and situations where the actor may need to establish a stronger or more recent authentication state.

The exact handling of such a result depends on the surrounding authentication and application flow.

## AccessOrchestrator

`AccessOrchestrator` coordinates the authorization process.

Its responsibilities include preparing the effective access context, resolving applicable policies, evaluating the required security conditions, and coordinating execution when access is permitted.

The conceptual pipeline is:

```text
Requested operation
        |
        v
Build AccessContext
        |
        v
Resolve authorization information
        |
        v
Evaluate invariants
        |
        v
Evaluate global policies
        |
        v
Resolve applicable runtime policies
        |
        v
Evaluate runtime policies
        |
        +---- Deny ------> Reject operation
        |
        +---- Reauth ----> Require reauthentication
        |
        v
      Allow
        |
        v
Execute authorized operation
```

This is a conceptual representation of the access decision pipeline, not a substitute for the concrete orchestration API.

The orchestrator centralizes authorization coordination so that individual application services do not need to reproduce the complete evaluation process.

## Authorization Before Execution

Authorization should be enforced before a protected operation performs security-sensitive changes.

Consider an administrative credential operation.

The system may need to establish:

1. Who is performing the operation.
2. Which account is being targeted.
3. Whether the actor possesses the required administrative permission.
4. Whether the target belongs to an allowed tenant.
5. Whether additional operation-specific restrictions apply.

Only after the applicable requirements succeed should the protected mutation proceed.

This separation is particularly important for operations that affect credentials, account status, roles, or sessions.

A rejected authorization decision must not leave behind a partially completed protected operation.

## Example: Preventing Administrative Self-Modification

Some administrative operations may intentionally prohibit an administrator from modifying their own account through an administrative route.

This is different from ordinary self-service account management.

A policy enforcing this restriction could consider:

- Whether the requested action has administrative scope.
- Whether the operation modifies security-sensitive account state.
- Whether the actor and target represent the same identity.

If the policy applies and the actor is also the target, it denies the operation.

The purpose is to prevent an administrative capability from bypassing the separate rules governing self-service operations.

Such a policy illustrates why permission checks alone are insufficient.

An administrator may possess the required action permission while still being prohibited from performing the operation against a particular target.

## Example: Requiring an Active User

An active-user policy evaluates whether the relevant identity satisfies the account state requirements for an operation.

An authenticated principal may exist even after the corresponding user account has been suspended or otherwise made ineligible.

The authorization system therefore needs a way to enforce account eligibility independently of the original authentication event.

The policy can evaluate the relevant user state and deny operations that require an active account.

Anonymous operations may follow different applicability rules because they do not necessarily have an authenticated user identity.

The exact enforcement behavior also depends on how current user security state is obtained.

## Policy Decisions and Authentication Modes

The policy model is conceptually independent of the authentication mode, but the freshness of its input data matters.

In session-authoritative modes, server-side state may be consulted as part of protected operations.

In JWT-based modes, some authorization information may originate from claims issued earlier.

This creates a distinction between:

- Evaluating a policy against current authoritative data.
- Evaluating a policy against previously issued authorization claims.

For example, a permission removed after token issuance may still be present in a locally validated JWT until that token expires.

A policy can evaluate its inputs correctly while those inputs reflect an earlier authorization state.

Applications must account for this when choosing token lifetimes, permission resolution strategies, and security requirements.

See [Authentication Modes](/docs/fundamentals/auth-modes).

## Application-Level Access Control

UltimateAuth's built-in authorization rules protect framework operations, but applications also have their own resources and business requirements.

A business application may need policies for:

- Editing resources owned by a particular user.
- Restricting operations to a tenant.
- Requiring elevated authority for administrative changes.
- Preventing sensitive changes under particular account conditions.

These requirements can be modeled as application-specific authorization policies.

The recommended architectural approach is to keep security decisions at a boundary that cannot be bypassed by alternative execution paths.

For HTTP endpoints, ASP.NET Core authorization can provide the request-level enforcement boundary.

For direct application service calls, including in-process operations, authorization must also be enforced where the protected operation actually executes.

The presence of an `[Authorize]` attribute on a controller does not automatically protect a service invoked directly from another part of the application.

## Extending the Policy Pipeline

UltimateAuth's policy model is designed to accommodate application-specific requirements without modifying the authentication engine.

Custom policies can introduce additional authorization conditions while participating in the established access decision process.

Such policies should preserve the framework's security boundaries:

- Do not treat untrusted request values as established identity facts.
- Do not bypass mandatory invariants.
- Do not use broad permission grants to override target or tenant restrictions.
- Keep policy applicability and evaluation behavior explicit.

For a complete, practical example of implementing and registering custom application policies, see [Define Authorization Policies](/docs/how-to-guides/define-authorization-policies). The guide demonstrates how to enforce Separation of Duties, compose multiple authorization requirements, and simplify application services.

## Architectural Perspective

UltimateAuth authorization is a coordinated evaluation of security requirements.

`AccessContext` describes the operation. Policy resolution identifies the relevant requirements. Policy evaluation determines whether those requirements are satisfied. `AccessOrchestrator` coordinates the process and its execution boundary.

The result is an authorization model that is **context-aware, policy-driven, and extensible without making individual permissions responsible for every security decision**.

## Next Steps

- [Authorization](/docs/plugin-domains/authorization) — Actions, permissions, roles, and scope semantics.
- [Users](/docs/plugin-domains/users) — Account lifecycle and identity state.
- [Credentials](/docs/plugin-domains/credentials) — Credential ownership and security operations.
- [Request Lifecycle](/docs/fundamentals/request-lifecycle) — Authorization within protected requests.