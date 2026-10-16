---
title: Auth-Flows Overview
order: 1
group: auth-flows
---

# 🔐 Auth Flows

Authentication is often described as a collection of endpoints: one accepts a password, another renews a token, and another clears a cookie. Those endpoints are useful interfaces, but they are not the security model.

**UltimateAuth treats authentication as a set of controlled flows operating within an explicitly resolved security context.** A flow represents an intention—establish access, continue it, or terminate it—and the server determines which rules, state, and delivery behavior apply before executing that intention.

This distinction matters because the same request can have different legitimate outcomes depending on the tenant, authentication mode, client profile, device, current session, and server policy. A flow should not have to guess those values independently.

## A flow is more than an endpoint

An endpoint receives an HTTP request. A flow describes what the request is attempting to do and provides a consistent context for the operation.

In UltimateAuth, the HTTP boundary, context resolution, domain orchestration, authorization decisions, persistence, and response delivery have separate responsibilities. This makes authentication behavior easier to reason about without forcing every flow to use exactly the same internal implementation.

> **Core principle:** Resolve the security context first. Make decisions against authoritative state. Commit only the changes permitted by the flow. Then deliver the outcome in the form appropriate for the client.

## The request-to-result pipeline

The following sequence describes the common **HTTP auth-flow boundary**. Individual flows have additional steps, and their internal order may differ.

```text
Incoming HTTP request
        |
        v
UltimateAuth Middleware Pipeline
  |
  +-- TenantMiddleware
  |     - Resolve tenant context
  |     - Store tenant context in HttpContext
  |
  +-- SessionResolutionMiddleware
  |     - Resolve session identifier
  |     - Build SessionContext (or anonymous context)
  |
  +-- UserMiddleware
  |     - Resolve user context through IUserAccessor
  |
  |-- ASP.NET Core Authentication and Authorization Middleware (Optional)
  |
  v
Request Context Prepared
        |
        v
Endpoint metadata (AuthFlowType)
        |
        v
AuthFlowEndpointFilter
        |
        v
AuthFlow.BeginAsync(...)
        |
        v
AuthFlowContextFactory
  - Read tenant, session, and user contexts
  - Resolve client profile and effective server options
  - Determine effective authentication mode
  - Resolve primary token kind and response delivery policy
  - Resolve device context and return URL
  - Validate existing session where applicable
  - Reject invalid or unresolved tenant
        |
        v
AuthFlowContext (request-scoped)
        |
        v
Endpoint handler and flow-specific validation
        |
        v
Flow service / orchestrator
        |
        v
Security and authorization decisions
        |
        v
Permitted state transitions and optional token operations
        |
        v
Response delivery, optional redirect, and flow events
```

This is a **conceptual pipeline**, not a promise that every flow creates a session, issues a token, or dispatches the same events. Nor is there one universal authority method that all flows invoke identically.

### 1. A request identifies the flow

UltimateAuth endpoints can carry `AuthFlowMetadata` describing the `AuthFlowType`. The `AuthFlowEndpointFilter` reads that metadata and invokes `IAuthFlow.BeginAsync(...)` before the endpoint handler executes.

This is an important boundary: the endpoint does not need to assemble its own authentication environment from scattered headers, cookies, and settings.

The filter initializes the flow context; it does **not**, by itself, validate a password, authorize every operation, or mutate the session store. Those responsibilities belong to the appropriate downstream components.

### 2. The server constructs `AuthFlowContext`

`AuthFlowContextFactory` resolves the inputs that govern how the operation must behave. The resulting context includes:

| Context value | Why it matters |
| --- | --- |
| `FlowType` | Identifies the intended authentication operation. |
| `Tenant` | Establishes the tenant boundary for state and decisions. |
| `ClientProfile` | Describes the kind of client interacting with the server. |
| `EffectiveMode` | Determines the authentication model actually in force for this request. |
| `Options` | Supplies the effective server policy. |
| `PrimaryTokenKind` | Identifies the primary credential/token kind for the effective mode. |
| `Device` | Carries the resolved device context. |
| `Session` | Carries validated session security context when available. |
| `IsAuthenticated` / `UserKey` | Represent the resolved authentication identity for this request. |
| `Response` | Describes effective credential delivery and redirect behavior. |
| `ReturnUrlInfo` | Carries parsed navigation intent for applicable browser flows. |

The factory rejects unresolved or invalid tenant context and disallowed authentication modes. When a session context is present, it invokes session validation. For session-authoritative modes such as `PureOpaque` and `Hybrid`, that validation also affects the resolved authentication state.

**The context is resolved, not blindly trusted.** Request-provided information may influence resolution, but the effective mode, policy, identity, and session validity are determined by server-side components.

### 3. The context is made available to the handler

`AuthFlow.BeginAsync` stores the created context through the request-scoped `AuthFlowContextAccessor`. The endpoint handler can then access the same resolved context rather than reconstructing it.

This creates a shared vocabulary across the HTTP boundary: the handler and downstream services work with the same tenant, mode, device, and response policy.

### 4. Flow-specific validation and orchestration begin

Once the context exists, the endpoint handler reads and validates the operation-specific request. For example, login needs an identifier and secret; logout needs an applicable session or target scope; refresh needs valid renewal material.

The handler delegates the actual operation to the relevant flow service or orchestrator. Those components coordinate credential checks, session operations, token operations, and state transitions as appropriate.

There is no benefit in pretending these operations are identical. **Their shared architecture is the context and security boundary; their individual state transitions remain purpose-specific.**

### 5. Decisions are evaluated against authoritative state

UltimateAuth distinguishes between what a client *presents* and what the system *knows*.

A cookie, bearer token, device identifier, or user-supplied value is evidence or input—not automatically the source of truth. The relevant authoritative data comes from server-managed user, credential, authentication-security, and session state, subject to the active authentication mode.

For session-backed flows, the hierarchy is:

```text
User
  └── Root
       └── Chain (device-associated lifecycle)
            └── Session (authentication instance)
```

The root, chain, and session provide distinct scopes for continuity, validation, and invalidation. Tokens and cookies can represent access to that state, but should not be confused with the underlying lifecycle.

**Mode distinction:** Do not assume that every mode has identical persistence or session-issuance behavior. In particular, `PureJwt` is not a session-issuance mode in the current session issuer. The effective mode determines which state and proof are authoritative for a given operation.

### 6. Only permitted transitions are committed

A successful authentication operation may create a session, rotate or touch existing state, revoke access, or leave persistent state unchanged. The actual effect depends on the flow and mode.

For example:

- Login verifies credentials and, in session-issuing modes, establishes a session before optionally issuing tokens.
- Refresh evaluates existing renewal authority and applies its configured continuation behavior; it is not simply a request to mint another token.
- Logout ends access at the relevant scope, while revoke represents irreversible invalidation of the revoked object or hierarchy scope.

Where a session command passes through `UAuthSessionOrchestrator`, `IAuthAuthority` makes an allow, deny, or challenge decision before the session issuer executes that command. Other operations use their own relevant decision and access orchestration paths.

This separation helps prevent HTTP transport details from becoming security policy.

### 7. The outcome is delivered according to policy

A successful domain operation and the HTTP response carrying its outcome are different concerns.

`AuthResponseResolver` derives the effective response configuration from the authentication mode, flow type, client profile, and server options. It binds applicable cookie settings and validates the delivery configuration before the endpoint performs the operation.

This means a flow can use the same underlying authentication semantics while producing a client-appropriate HTTP outcome—for example, a response with managed cookies or a browser redirect when configured.

**Current implementation note:** `TokenResponseMode.Body` credential delivery is explicitly unsupported by the response resolver. Do not assume that access or refresh credentials are returned in a JSON response body.

A redirect is a navigation decision, **not** an authentication or revocation mechanism. It can return a browser to the application after login or logout; the underlying security operation must stand independently of whether the browser follows the redirect.

## Why `AuthFlowContext` matters

Without a shared flow context, individual endpoints tend to make their own assumptions: which tenant is active, whether a cookie represents an authenticated session, which token type is expected, whether a client should receive cookies, and where to navigate afterward.

Those assumptions drift as applications add multiple clients, devices, tenants, or authentication modes.

UltimateAuth instead resolves these questions at the flow boundary. That provides three practical benefits:

1. **Consistency.** The same effective policy is available throughout the operation.
2. **Separation of concerns.** Endpoint handlers handle HTTP; orchestrators coordinate security-relevant work; response components handle delivery.
3. **Extensibility.** New client profiles and flow behaviors can integrate with established boundaries instead of duplicating authentication plumbing.

A context is not an authorization bypass. Possessing an `AuthFlowContext` does not imply that the requested operation has been approved.

## Source of truth: proof versus state

A useful mental model is to distinguish **proof**, **policy**, and **state**:

| Concept | Examples | Responsibility |
| --- | --- | --- |
| Proof | Password, session identifier, access token, refresh token | Presents evidence for an operation. |
| Policy | Effective mode, server options, authority decisions | Determines what is permitted. |
| State | User eligibility, credential records, security state, session root/chain/session | Determines the authoritative lifecycle and validity where applicable. |
| Delivery | Cookies, supported response modes, redirect | Communicates the result to the client. |

A valid-looking token is not, by itself, permission to ignore revocation, tenant boundaries, session state, or mode-specific validation. Conversely, not every authentication mode is built around a persisted session hierarchy. The authoritative checks must match the configured model.

This distinction is central to UltimateAuth's vision: **authentication is not the possession of a string; it is the outcome of a controlled security process.**

## Built-in flows at a glance

The following pages explain each operation in detail. This overview focuses on their roles within the shared model.

| Flow | Purpose | Practical outcome | Guide |
| --- | --- | --- | --- |
| **Login** | Establish authenticated access from acceptable credentials. | Creates a valid authentication session in session-issuing modes; may issue tokens according to policy. | [Login Flow](./login-flow.md) |
| **Refresh** | Continue an existing authentication relationship using renewal authority. | Maintains or renews access according to mode, validation, and configured refresh behavior. | [Refresh Flow](./refresh-flow.md) |
| **Logout** | End access at the applicable session or device scope. | Terminates authentication access and handles configured client cleanup/navigation. | [Logout Flow](./logout-flow.md) |

These flows are related but not interchangeable. Login establishes access; refresh continues it; logout terminates it. Revocation operations provide stronger invalidation controls for the relevant security scope.

## Concepts shared by the flows

| Concept | What it contributes | Guide |
| --- | --- | --- |
| **Session lifecycle** | Root, chain, and session relationships; creation, continuity, expiry, and invalidation. | [Session Lifecycle](./session-lifecycle.md) |
| **Token behavior** | Access and refresh credentials, validation, renewal, and mode-specific behavior. | [Token Behavior](./token-behavior.md) |
| **Device management** | Device-associated chains, multi-device access, and targeted termination. | [Device Management](./device-management.md) |

## What this design enables in real applications

Consider an application with a browser front end, a mobile client, and an administrative interface. They may share the same user identity system, but they do not necessarily have the same credential delivery needs or session behavior.

A flow-driven architecture lets the server establish the applicable context first and then run the requested operation under that context. Developers can reason about **what authentication action is taking place** separately from **how the resulting credentials or navigation are delivered**.

It also makes security-sensitive behavior easier to locate: tenant and mode resolution occur at the boundary, domain validation happens in the flow, session operations use dedicated infrastructure, and response policy is resolved independently.

This is the larger goal of UltimateAuth's unified model—not to make every authentication operation identical, but to make their differences explicit, controlled, and composable.

## Key takeaways

- **An auth flow is a controlled operation**, not merely an HTTP endpoint.
- **`AuthFlowEndpointFilter` initializes the request context** for endpoints carrying flow metadata.
- **`AuthFlowContext` resolves tenant, mode, client profile, device, identity/session information, and response policy** before the handler executes.
- **Proof is not the source of truth.** Validation depends on authoritative state and the effective authentication mode.
- **Authority and orchestration are distinct responsibilities.** The exact checks and state transitions vary by flow.
- **Credential delivery and redirects are outcomes**, not substitutes for security decisions.
- **Login, refresh, and logout share a model without sharing identical execution paths.**

## Next step

Start with [Login Flow](/docs/auth-flows/login) to see how UltimateAuth turns validated credentials into controlled authenticated access.
