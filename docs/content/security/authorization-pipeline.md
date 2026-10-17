---
title: Authorization Pipeline Internals
order: 4
group: security
---

# Authorization Pipeline Internals

UltimateAuth authorization is implemented as a coordinated evaluation process rather than a collection of unrelated permission checks.

The authorization pipeline brings together the operation being requested, the identities involved, the applicable permissions, and the policies responsible for enforcing security requirements.

At its center are several distinct responsibilities:

- **Context preparation:** Establish the security information required to evaluate an operation.
- **Permission resolution:** Make effective authorization grants available to the decision process.
- **Policy resolution:** Determine which security rules apply to the requested action.
- **Policy evaluation:** Evaluate the applicable requirements in a defined order.
- **Decision enforcement:** Permit the protected operation to proceed only when the authorization result allows it.

This page examines how those responsibilities fit together.

For an introduction to actions, permissions, and roles, see [Authorization](/docs/plugin-domains/authorization). For policy concepts and their intended use, see [Policies & Access Control](/docs/plugin-domains/policies).

## Pipeline Architecture

The authorization process can be represented as a sequence of stages:

```text
Protected Operation
        |
        v
  AccessContext
        |
        v
Context Enrichment
        |
        v
Permission Resolution
        |
        v
Invariant Evaluation
        |
        v
Global Policy Evaluation
        |
        v
Runtime Policy Resolution
        |
        v
Runtime Policy Evaluation
        |
        v
  Final Decision
        |
        +---- Deny ----------------> Reject
        |
        +---- Reauthentication ---> Require Reauthentication
        |
        +---- Allow ---------------> Execute
```

This diagram describes the logical responsibilities of the pipeline.

It should not be interpreted as a guarantee that every stage performs a separate database query or that every authentication mode uses the same mechanism to obtain authorization information.

The important property is that **authorization is evaluated against a coherent access context before the protected operation is allowed to proceed**.

## AccessContext as the Evaluation Boundary

`AccessContext` carries the information required to evaluate an authorization request.

It connects the authenticated actor, the intended target, the requested action, and the relevant security environment.

Conceptually, its responsibilities include:

| Information | Purpose |
|---|---|
| Actor | Identifies the principal performing the operation |
| Target | Identifies the account or resource affected |
| Action | Describes the protected operation |
| Tenant context | Establishes the relevant isolation boundaries |
| Claims | Provides authentication and authorization information |
| Permissions | Represents the effective permission information available for evaluation |

The context is shared across the authorization process so that individual policies do not need to independently reconstruct the operation.

### Actor and Target Separation

Consider the action:

```text
users.delete.admin
```

The actor is the principal requesting the deletion.

The target is the user account being deleted.

These identities may be different, and the authorization pipeline must preserve that distinction.

For a self-service action:

```text
users.delete.self
```

The action indicates a self-service operation, but the action name alone does not prove that actor and target are the same identity.

That relationship must be established from trusted context and evaluated by the applicable security rules.

### Context Integrity

The reliability of the authorization decision depends on the integrity of the context.

A target identifier or tenant identifier supplied by a client must not automatically be accepted as a trusted authorization fact.

The pipeline must operate on the authenticated actor and appropriately resolved target information.

A correctly implemented policy cannot compensate for a context that incorrectly identifies the actor or misrepresents the target.

## Context Enrichment

The initial authorization context may not contain all the information required by the applicable policies.

Context enrichment makes additional authorization information available before evaluation.

This can include resolving effective permissions and preparing them for efficient policy checks.

Conceptually:

```text
Authenticated Actor
        |
        v
Resolve Authorization Information
        |
        v
Effective Permissions
        |
        v
CompiledPermissionSet
        |
        v
Enriched AccessContext
```

The purpose of enrichment is to separate authorization data resolution from the individual rules that consume that data.

A permission policy should not need to independently load role assignments and rebuild the user's effective permission set every time it evaluates an action.

### Authorization Data Freshness

Enrichment does not automatically guarantee that all authorization information is current.

The source of permission data matters.

For example, permissions obtained from current server-side state may reflect a recent role change, while permissions derived exclusively from an already-issued JWT may reflect an earlier state.

The pipeline can evaluate the information it receives consistently, but it cannot make stale claims current without consulting an authoritative source.

This distinction is particularly relevant to SemiHybrid and PureJwt authentication.

## Permission Compilation

UltimateAuth uses `CompiledPermissionSet` to represent permissions in a form suitable for repeated authorization checks.

Permission compilation separates the representation of stored grants from the process of deciding whether an action is permitted.

For example:

```text
Requested action:
    users.delete.admin

Effective grants:
    users.read.self
    users.delete.admin
    credentials.list.self
```

A permission policy can evaluate whether the effective grants cover the requested action.

Conceptually:

```csharp
permissionSet.IsAllowed(action);
```

This illustrates the permission-checking responsibility, rather than a complete public API integration example.

### Wildcard Permissions

Compiled permissions may also represent wildcard grants.

For example:

```text
users.*
users.profile.*
*
```

Wildcard matching must respect the permission model's defined action hierarchy.

A broad grant should not be inferred merely because several currently visible permissions share a prefix.

Where permission normalization replaces a complete action group with a wildcard, the normalization must preserve the intended permission semantics.

### Compilation Is Not Authorization

A compiled permission set is an efficient representation of grants.

It does not independently establish that the actor may perform the operation.

A matching permission can still be insufficient when:

- The actor is not permitted to access the target.
- The operation crosses a prohibited tenant boundary.
- The account does not satisfy required security conditions.
- An operation-specific policy denies the action.

Permission matching is one requirement within the broader authorization decision.

## Policy Registration and Resolution

Runtime policies must be discoverable for the operations they protect.

UltimateAuth's authorization architecture includes policy registration and compilation concepts represented by:

- `AccessPolicyRegistry`
- `CompiledAccessPolicySet`

These components separate policy configuration from runtime selection.

Conceptually:

```text
Registered Policies
        |
        v
AccessPolicyRegistry
        |
        v
CompiledAccessPolicySet
        |
        v
Policies Matching Requested Action
```

### Action-Based Selection

Policies may be associated with action prefixes or patterns.

For example:

```text
users.*
users.delete.*
users.delete.admin
```

A request for:

```text
users.delete.admin
```

may select policies associated with the relevant action hierarchy.

This allows broad policies to cover a domain while more specific policies enforce requirements for individual operations.

### Prefix Matching and Boundaries

Action matching must respect the intended structure of action identifiers.

For example:

```text
users.delete.admin
```

belongs to the `users` action domain.

But an unrelated action whose text merely begins with similar characters should not automatically be treated as belonging to that domain.

Policy matching must therefore follow the actual action-pattern semantics implemented by the registry.

### Resolution Versus Evaluation

Policy resolution answers:

**Which policies are candidates for this operation?**

Policy evaluation answers:

**Do those policies permit this operation in the current context?**

A policy may match an action pattern but still be inapplicable to a particular runtime context.

That distinction is handled through policy applicability rules.

## Runtime Applicability

A policy can determine whether it applies to the current authorization context.

UltimateAuth represents this responsibility through `AppliesTo`.

For example, a policy restricting administrative self-modification may only be relevant when:

- The requested action has administrative scope.
- The operation changes protected account state.
- The actor and target are the same identity.

It may not apply to administrative read operations or unrelated domains.

Conceptually:

```text
Policy selected by action
        |
        v
AppliesTo(context)
        |
        +---- No ----> Skip policy
        |
        +---- Yes ---> Evaluate policy
```

This enables policies to remain focused on specific security requirements without requiring every action to have an entirely separate policy implementation.

### Applicability Is Not a Security Bypass

An applicability rule determines when a policy participates in evaluation.

If a mandatory security requirement can be skipped because its applicability condition is too broad or incorrectly implemented, the resulting authorization boundary may be weaker than intended.

Policies enforcing fundamental security invariants should not depend on application-controlled applicability rules that can bypass those invariants.

## Policy Evaluation Stages

UltimateAuth's policy architecture distinguishes between invariants, global policies, and runtime policies.

These categories establish the order and scope of security evaluation.

### Stage 1: Invariants

Invariants represent mandatory security requirements.

They are evaluated before ordinary policy stages.

An invariant denial prevents the requested operation from proceeding, regardless of whether another policy would otherwise permit it.

Invariants provide the strongest enforcement boundary within the policy pipeline.

They should not be treated as ordinary permissions that can be overridden by a broader grant.

### Stage 2: Global Policies

Global policies enforce broadly applicable security requirements.

They may examine the operation's context to determine whether their rule is relevant.

Examples include requirements related to authentication state, account eligibility, and tenant isolation.

Global policies allow these requirements to be enforced consistently without attaching identical rules to every individual action.

### Stage 3: Runtime Policies

Runtime policies are resolved according to the requested action and evaluated against the current context.

They support requirements such as:

- Required permissions.
- Self-access restrictions.
- Administrative operation constraints.
- Domain-specific authorization rules.

Runtime policies extend the decision process without replacing the mandatory security requirements established by earlier stages.

## IAccessAuthority and Decision Evaluation

`IAccessAuthority` represents the authorization decision responsibility within the pipeline.

The authority evaluates the applicable security requirements and produces an authorization result.

Conceptually, the result can represent:

| Result | Meaning |
|---|---|
| **Allow** | The applicable requirements permit the operation |
| **Deny** | A required security condition rejects the operation |
| **Reauthentication required** | The operation requires an additional authentication condition before proceeding |

These outcomes must not be confused with the result of an individual policy.

### Policy Allow Versus Final Allow

An individual policy may return an allow result because its own requirement has been satisfied.

That does not necessarily mean the entire operation is authorized.

Other applicable policies may still deny the request or require reauthentication.

**An individual policy's approval is not a universal authorization grant.**

### Deny Precedence

A denial from an applicable mandatory policy prevents the operation from being authorized.

The pipeline must not allow a later permissive policy to override a previously established denial.

This supports predictable enforcement across multiple independent security rules.

### Reauthentication Requirements

Some operations may require stronger or more recent authentication.

A policy can represent that requirement separately from an ordinary denial.

The authorization system must distinguish between:

- An operation that is prohibited.
- An operation that may proceed after additional authentication.
- An operation that is currently authorized.

The exact precedence and propagation of reauthentication results must follow the implemented decision engine.

A reauthentication result should not be interpreted as permission to execute the protected operation immediately.

## Permission Policies

`MustHavePermissionPolicy` represents the connection between the permission model and the policy pipeline.

Its responsibility is to evaluate whether the actor has the permission required for the requested action.

Conceptually:

```text
AccessContext.Action
        |
        v
CompiledPermissionSet
        |
        v
Permission Evaluation
        |
        v
Policy Decision
```

This design separates permission storage and resolution from permission enforcement.

Roles and assignments determine effective grants.

The compiled permission set represents those grants.

The permission policy evaluates the requested action against them.

The final authorization decision also considers the other applicable security policies.

### Permissions Are Not a Replacement for Policies

For example, an actor may possess:

```text
users.delete.admin
```

That permission does not automatically authorize deleting any user in any tenant.

The operation must still satisfy the relevant target, tenant, and account security requirements.

This is why the pipeline evaluates multiple requirements rather than relying on a single permission match.

## Orchestration and Protected Execution

`AccessOrchestrator` coordinates authorization and the protected operation.

Its purpose is to establish a consistent enforcement boundary.

Conceptually:

```text
Operation Requested
        |
        v
Prepare AccessContext
        |
        v
Resolve and Evaluate Policies
        |
        +---- Not Allowed ---> Stop
        |
        v
Execute Protected Operation
```

The security requirement is straightforward:

**A protected operation must not execute merely because its caller reached an application service or endpoint.**

Authorization must succeed at an enforcement boundary that covers the operation.

### HTTP and In-Process Execution

HTTP endpoints may enforce authorization through ASP.NET Core authentication and authorization integration.

However, applications may also execute operations directly through in-process services.

For example, a Blazor Server application can invoke a server-side service without making a separate HTTP request.

A controller-level `[Authorize]` attribute does not automatically protect such a direct service invocation.

Where operations can be reached through multiple execution paths, authorization must be enforced at an appropriate shared boundary.

This prevents one entry point from bypassing rules enforced by another.

## Determinism and Security

A structured policy pipeline makes authorization behavior easier to reason about.

For the same effective context, policy configuration, and authoritative security state, the decision should follow the same evaluation rules.

However, authorization can depend on mutable information:

- Account status.
- Tenant membership.
- Role assignments.
- Effective permissions.
- Session security state.
- Target ownership.

A policy evaluation that uses stale or inconsistent data may produce a result that differs from one using current authoritative state.

Deterministic policy evaluation therefore depends on both the evaluation rules and the consistency of the information supplied to them.

### Time-of-Check and Time-of-Use

Authorization may be evaluated before a protected operation modifies data.

If relevant security or ownership information changes between the authorization decision and the mutation, a time-of-check/time-of-use problem may arise.

Sensitive operations may require transactional consistency, concurrency controls, or revalidation within the protected operation.

A policy pipeline provides a structured decision process, but it does not automatically make every application-level data mutation atomic.

## Extensibility Boundaries

The authorization pipeline is designed to support application-specific policies.

Custom policies can introduce new requirements without replacing the underlying permission model or authentication engine.

However, extensibility must preserve the security hierarchy.

A custom policy should not:

- Override mandatory invariants.
- Trust caller-supplied identity information without validation.
- Treat an action name as proof of actor-target ownership.
- Bypass tenant isolation through a broad permission.
- Assume that JWT claims always reflect current authorization state.

Custom policies should evaluate well-defined requirements using trusted context and participate in the established decision model.

## Architectural Perspective

UltimateAuth's authorization pipeline separates the responsibilities involved in an access decision.

`AccessContext` describes the operation.

Context enrichment and permission compilation prepare the required authorization information.

Policy resolution identifies the relevant rules.

`IAccessAuthority` evaluates those rules according to the pipeline's decision semantics.

`AccessOrchestrator` coordinates authorization with protected execution.

Together, these components provide a structured foundation for context-aware, policy-driven authorization.

**The pipeline's security comes not from the number of policies it executes, but from consistently enforcing the applicable requirements before a protected operation is allowed to proceed.**

## Next Steps

- [Authorization](/docs/plugin-domains/authorization) — Actions, permissions, roles, and scopes.
- [Policies & Access Control](/docs/plugin-domains/policies) — Policy concepts and access decision behavior.
- [Session Security Model](/docs/security/session-security) — Authentication security state and validation boundaries.
- [Token Security](/docs/security/token-security) — Credential validity and authorization freshness.