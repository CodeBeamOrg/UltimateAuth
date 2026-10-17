---
title: Flow-Based Authentication
order: 3
group: fundamentals
---

# Flow-Based Authentication

UltimateAuth organizes authentication around explicit, controlled operations called **authentication flows**.

A flow represents an authentication intention, such as establishing access, continuing an existing authentication context, or terminating it.

Rather than allowing individual endpoints or client applications to independently decide how authentication should work, UltimateAuth evaluates each operation within a consistent, server-controlled security context.

This approach is independent of the credential format.

A flow may work with a server-side session, an opaque access token, a JWT, or a refresh token. The effective authentication mode determines which credentials and validation rules apply.

**Flow-based authentication is not an alternative to session-based or token-based authentication. It is the architecture that coordinates those strategies.**

## Why authentication flows matter

Authentication involves more than issuing and validating credentials.

A complete authentication operation may need to determine:

- Which tenant the operation belongs to.
- Which authentication mode is permitted.
- Which client profile and runtime characteristics apply.
- What authentication proof is required.
- Whether an existing authentication context can continue.
- Which security policies must be enforced.
- Whether server-side state must be created, updated, or revoked.
- Which credentials or other results should be returned to the client.

These decisions should not be independently reconstructed by every endpoint, client SDK, or authentication service.

UltimateAuth establishes a shared context for the operation and delegates the relevant decisions to the components responsible for them.

The result is a predictable authentication process whose behavior can vary by mode without changing its fundamental security boundaries.

## What is an authentication flow?

An authentication flow is a controlled operation with a defined purpose, applicable security requirements, and an explicit outcome.

Three primary flows illustrate the model.

### Login

Login establishes a new authenticated context after the required authentication proof has been accepted.

Depending on the effective mode, the operation may create server-side authentication state, issue access credentials, or perform both actions.

### Refresh

Refresh attempts to continue an existing authenticated context without requiring the original authentication proof again.

Its behavior depends on the mode.

For example, PureOpaque can continue a session through touch semantics, while token-based modes may issue replacement credentials.

Refresh must not bypass expiration, revocation, or other continuation requirements.

### Logout

Logout terminates or discards the applicable authentication context.

In session-authoritative modes, this can involve revoking server-side sessions.

In stateless JWT authentication, client-side credential removal does not automatically invalidate an already issued access token.

These operations share architectural boundaries but do not perform identical work.

For their implementation details, see [Auth Flows](/docs/auth-flows/overview).

## The flow execution model

At a conceptual level, an authentication flow follows a common progression:

```text
Authentication Intent
         |
         v
Resolve Effective Security Context
         |
         v
Validate Required Evidence
         |
         v
Evaluate Applicable Security Rules
         |
         v
Execute Mode-Specific Operation
         |
         v
Produce Authentication Outcome
         |
         v
Deliver Result to Client
```

This diagram represents responsibilities, not a fixed implementation sequence.

For example, credential validation, authority decisions, and state transitions may involve different components or ordering depending on the operation.

Some flows create sessions. Others update existing state, rotate tokens, revoke credentials, or perform no persistent state change.

The important guarantee is that each operation executes within a controlled context and follows the security rules applicable to its purpose and authentication mode.

## AuthFlowContext

A central concept in this architecture is `AuthFlowContext`.

`AuthFlowContext` represents the effective authentication environment in which an operation executes.

It brings together the information required to apply consistent authentication behavior, including:

- The intended authentication flow.
- The effective authentication mode.
- The resolved tenant context.
- The applicable client profile.
- Effective server options and security policies.
- Relevant device and session context, when applicable.
- Credential delivery and response behavior.

The context is established through framework-controlled boundaries.

Once established, it must remain stable throughout the operation. Individual services and endpoints must not independently replace it or reconstruct it using different assumptions.

This matters particularly in applications serving multiple tenants, client runtimes, and authentication modes.

A flow must not begin under one security policy and complete under another merely because different components interpreted the request differently.

### Context is not authorization

`AuthFlowContext` describes the environment and rules under which an operation executes.

Its existence does not mean that the operation has been authorized or that the presented credentials are valid.

The relevant authentication evidence and security requirements must still be evaluated.

After authentication has established trusted identity, authorization can operate through the appropriate `AccessContext`.

The distinction between these contexts is explained in [Authentication Model](/docs/fundamentals/auth-model).

## Flows and authentication modes

Authentication flows describe **what operation is being performed**.

Authentication modes determine **how the operation establishes or continues trust**.

Consider login.

In PureOpaque, a successful login establishes a server-side session and provides a session credential.

In Hybrid, login establishes server-side session authority and issues separate opaque access and refresh tokens.

In SemiHybrid, login establishes the session lifecycle state required by the mode and issues JWT-based access credentials.

In PureJwt, login issues JWT credentials without requiring a server-side session.

All four are login operations, but their persistence and credential-issuance requirements differ.

The same principle applies to refresh and logout.

| Flow | What remains consistent | What varies by mode |
|---|---|---|
| Login | Establish authenticated access from validated evidence | Session creation, token format, credential issuance |
| Refresh | Determine whether authentication may continue | Session touch, session validation, token rotation, renewal state |
| Logout | End or discard the applicable authentication context | Session revocation, refresh-token invalidation, client credential cleanup |
| Request authentication | Establish trusted identity from presented credentials | Session validation, opaque token resolution, JWT validation |

A flow must never introduce a session dependency merely because another authentication mode requires one.

In particular, PureJwt must remain independent of mandatory session persistence and validation.

For the complete mode definitions and their guarantees, see [Authentication Modes](/docs/fundamentals/auth-modes).

## Server-controlled decisions

UltimateAuth treats authentication as a server-controlled process.

The client can request an operation and provide the evidence needed to perform it.

However, the client does not independently determine:

- Whether the authentication evidence is acceptable.
- Which security policies can be bypassed.
- Whether an expired or revoked authentication context may continue.
- Whether a new session or token may be issued.
- Whether a particular authentication mode is permitted.

These decisions belong to trusted framework and application components operating under the effective server configuration.

This does not mean every authentication decision requires a database lookup.

For stateless JWT authentication, the server can establish identity through cryptographic verification and the applicable token-validation rules.

For session-authoritative authentication, the server must also validate the required session state.

Server authority describes **who controls the rules**, not whether every request must consult persistent storage.

## Separation of responsibilities

Flow-based authentication is also a separation-of-concerns model.

The components participating in an authentication operation have distinct responsibilities.

**Request and context resolution**

Establish the trusted execution environment, including tenant, mode, client profile, and effective policies.

**Authentication validation**

Evaluate the evidence required by the operation and authentication mode.

**Security decisions**

Determine whether the requested operation is permitted under the applicable rules and authoritative state.

**Orchestration**

Coordinate the services and state transitions needed to complete the operation.

**Persistence and credential issuance**

Create, update, or revoke the relevant authentication state and credentials when required.

**Response delivery**

Communicate the result through the appropriate cookie, header, redirect, or other supported delivery mechanism.

Separating these responsibilities prevents transport behavior from becoming authentication policy.

For example, whether an access token is delivered through a cookie or header should not change the security requirements used to issue it.

The technical components and request pipeline implementing these responsibilities are documented in [Auth Flows](../auth-flows/).

## Flows across client runtimes

UltimateAuth supports different application environments through a common authentication architecture.

A browser application, Blazor Server application, Blazor WebAssembly application, mobile client, and resource API may interact with authentication differently.

For example:

- A browser application may rely on secure cookies.
- A mobile application may manage credentials through secure device storage.
- A resource API may validate bearer access tokens.
- A Blazor Server application may use a server-controlled session without managing refresh-token rotation.

These differences affect client integration and credential transport.

They do not require independent implementations of the fundamental authentication model.

Client Profiles provide runtime-appropriate behavior and defaults, while authentication modes define the validation and lifecycle strategy.

See [Client Profiles](/docs/fundamentals/client-profiles) for how runtime characteristics influence authentication configuration.

## Predictable outcomes

An authentication flow should produce an explicit result that reflects what the operation actually accomplished.

For example, a refresh operation may:

- Continue an existing session without changing persistent state.
- Update session activity or expiration when permitted.
- Rotate refresh credentials and issue replacement tokens.
- Reject continuation and require reauthentication.

These outcomes are not interchangeable.

A successful operation should not imply that a token was issued when no token was needed.

Likewise, an unsuccessful refresh must not silently create a new authenticated context or bypass the original authentication requirements.

Explicit outcomes allow the client SDK and server components to respond consistently without assuming that every mode behaves the same way.

## Extensibility and security boundaries

A flow-based architecture provides controlled extension points for authentication behavior.

Depending on the supported features and configured services, applications may integrate additional requirements such as:

- Multi-factor authentication.
- Additional credential verification.
- Tenant-specific authentication policies.
- Device-related security checks.
- Custom authentication proof providers.
- Additional security decisions before issuing or continuing access.

Extensions must preserve the security guarantees of the effective authentication mode.

For example, a custom flow must not bypass mandatory session validation in Hybrid.

Similarly, an extension must not make session persistence an undocumented prerequisite for PureJwt.

Flexibility must not silently change the meaning of an authentication mode.

## Flow-based does not mean session-based

An important distinction is that authentication flows and authentication lineage are related but separate concepts.

**Authentication flows** describe controlled operations.

**Authentication lineage** describes the Root, Chain, and Session relationships used by session-backed authentication.

A flow may operate on that lineage when required.

However, not every flow or mode requires persisted Root, Chain, or Session state.

This separation allows UltimateAuth to support both stateful and stateless authentication without maintaining two unrelated authentication architectures.

See [Authentication Model](/docs/fundamentals/auth-model) for the lineage model and its role in authentication.

## Summary

UltimateAuth uses authentication flows to coordinate security-sensitive operations through a consistent, trusted execution context.

A flow:

- Represents an explicit authentication intention.
- Operates under a resolved and stable `AuthFlowContext`.
- Applies the validation and security rules required by the effective mode.
- Coordinates only the state transitions and credential operations necessary for its purpose.
- Produces an explicit outcome.
- Delivers that outcome according to the applicable client and response configuration.

The architecture does not require every mode to use the same credentials, persistence model, or validation strategy.

**Flows define the operation. Modes define its authentication behavior. Client Profiles adapt its integration with the runtime.**

## Next step

Continue to [Authentication Modes](/docs/fundamentals/auth-modes) to understand how PureOpaque, Hybrid, SemiHybrid, and PureJwt implement different authentication and session lifecycle strategies.