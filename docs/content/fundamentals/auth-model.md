---
title: Authentication Model
order: 2
group: fundamentals
---

# Authentication Model

UltimateAuth is built around a fundamental principle: **authentication is a trusted process for establishing identity, not simply the presence of a token, cookie, or session identifier.**

An authentication credential provides evidence of identity. The server must evaluate that evidence according to the active authentication mode before treating a request as authenticated.

UltimateAuth provides a common authentication architecture that supports both server-side sessions and cryptographically verifiable tokens.

The authentication mode determines how identity is established, what state must be validated, and which lifecycle guarantees apply.

## Authentication and identity

Authentication answers a specific question:

**Who is making this request, and what evidence establishes that identity?**

An authentication operation may involve credentials such as passwords, existing sessions, access tokens, refresh tokens, or other supported authentication proofs.

These credentials have different purposes.

A password can establish a new authenticated context. An access token can represent an already established identity. A refresh token can continue an existing authentication context without repeating the original login.

None of these credentials should be trusted merely because a client presents them.

UltimateAuth evaluates authentication evidence through framework-controlled services and produces a trusted identity context only after the applicable validation succeeds.

Authorization is a separate responsibility. It determines what an authenticated actor is allowed to do.

## Server authority

UltimateAuth is server-authoritative.

This means the server defines the authentication rules and determines whether the presented evidence satisfies them.

Server authority does not require every request to access a server-side session store.

For example:

- In PureOpaque, the server validates the session represented by the client's opaque session credential.
- In Hybrid, the server resolves an opaque access token and validates its associated session.
- In SemiHybrid, normal requests authenticate through JWT validation, while server-side sessions govern authentication continuation and lifecycle operations.
- In PureJwt, authentication can be established through JWT validation without requiring session persistence or lookup.

In all cases, the effective authentication mode, validation requirements, and security policies remain under trusted server control.

The client cannot establish its own authenticated identity merely by supplying claims, identifiers, or an arbitrary authentication mode.

## Authentication state

Authentication state can be represented in different ways.

**Server-side authentication state** is maintained by the authentication system and evaluated when required. It enables centralized session management and immediate session-based revocation.

**Token-based authentication state** can be represented by cryptographically verifiable credentials. A JWT can carry sufficient information to establish identity without a session lookup, provided its signature and applicable validation rules are satisfied.

**Combined authentication state** uses both approaches for different purposes. For example, an application can validate JWTs independently while retaining server-side state for refresh and session management.

These are different authentication strategies, not separate authentication architectures.

UltimateAuth preserves common trust boundaries while allowing the effective mode to determine how authentication state is represented and evaluated.

## Authentication lineage

For session-backed authentication, UltimateAuth models authentication state through three related concepts:

- **Root:** User-level authentication authority within a tenant.
- **Chain:** A grouping of related authentication sessions, typically associated with a device or client context.
- **Session:** An individual authenticated session.

Together, these form the authentication lineage.

```text
Root
  |
  +-- Chain A
  |     |
  |     +-- Session 1
  |     |
  |     +-- Session 2
  |
  +-- Chain B
        |
        +-- Session 3
```

The lineage provides a structured way to manage authentication state at different scopes.

It allows the system to distinguish between terminating one session, terminating sessions associated with a particular chain, and applying a broader user-level security change.

The lineage is central to session-backed authentication. It must not be interpreted as a mandatory persistence requirement for every authentication mode.

In particular, PureJwt does not require Root, Chain, or Session records to authenticate requests.

## Root: user-level authentication authority

Root represents the user-level authority within a tenant's session-backed authentication lineage.

It provides a common security boundary for related chains and sessions.

Depending on the configured policies and supported operations, Root can participate in:

- Tracking user-level authentication security state.
- Coordinating security changes affecting multiple sessions.
- Applying broader session invalidation.
- Maintaining a security version or epoch used by session-backed validation.

For example, a security-sensitive credential change may trigger a policy that invalidates existing sessions.

Rather than treating each session as unrelated, the lineage provides a common authority through which the affected authentication state can be managed.

### Security versions

A security version, sometimes called a security epoch, can be used to determine whether an existing session still belongs to the current authentication security state.

A session may retain the version associated with its creation. When the relevant authority changes, validation can reject sessions associated with an outdated version.

This mechanism is meaningful when authentication validation consults the relevant server-side state.

It does not automatically invalidate a previously issued JWT that is validated without a server-side lookup.

The exact invalidation behavior depends on the authentication mode and configured security policies.

## Chain: related session context

Chain groups related authentication sessions under a common lifecycle context.

A chain commonly represents a device or client context, but its purpose is broader than storing a device identifier.

It provides a boundary for managing related authentication activity.

A chain can support:

- Grouping related sessions.
- Device-oriented session management.
- Chain-level expiration and invalidation.
- Coordinating refresh-token lifecycle where applicable.
- Applying security policies to related authentication sessions.

For example, an application may allow a user to terminate authentication associated with a particular device without terminating every session belonging to the user.

A chain provides a suitable scope for that operation.

A chain is not itself proof of authentication. Its validity and relationship to the relevant session must be evaluated according to the active mode.

## Session: an authenticated instance

Session represents an individual authenticated instance maintained by the server.

It can contain or reference the information required to manage that instance, such as:

- The authenticated user and tenant.
- Its relationship to a chain.
- Creation and expiration information.
- Revocation or lifecycle status.
- Applicable security-version information.
- Session metadata required by supported policies.

The role of Session differs by authentication mode.

### Session-authoritative authentication

In PureOpaque and Hybrid, session validity is part of the authentication decision.

A request cannot be treated as authenticated merely because it presents a structurally valid credential. The associated session must also satisfy the applicable validation requirements.

In these modes, session invalidation affects subsequent authentication evaluations.

### Session-managed authentication

In SemiHybrid, sessions support lifecycle operations such as refresh, logout, device management, and session revocation.

However, normal JWT authentication does not require session validation.

Consequently, revoking a session can prevent future refresh operations without automatically invalidating an already issued JWT.

### Session-independent authentication

PureJwt does not require a Session record.

Authentication relies on cryptographic token validation and applicable claims and token-validation policies.

There is no mandatory session authority or session lookup in its authentication pipeline.

## Establishing a new authentication context

Authentication begins when the system receives evidence sufficient to establish a new authenticated identity.

A typical login operation follows these conceptual steps:

1. Resolve the effective authentication context and security policies.
2. Validate the presented authentication proof.
3. Establish the authenticated identity.
4. Create the authentication state required by the selected mode.
5. Issue the appropriate authentication credentials.
6. Deliver those credentials through the configured transport mechanism.

The fourth and fifth steps are mode-dependent.

For session-backed modes, establishing authentication can involve creating or resolving the relevant Root, Chain, and Session state.

For PureJwt, issuing a JWT must not require creating a server-side session.

The shared authentication pipeline does not imply that every mode performs identical persistence operations.

## Authenticating subsequent requests

After login, a client presents the credential appropriate for its authentication mode.

The request pipeline validates that credential and establishes the trusted authentication state.

```text
Incoming Request
       |
       v
Effective Authentication Context
       |
       v
Credential Resolution
       |
       v
Mode-Specific Authentication Validation
       |
       +-- PureOpaque: Session validation
       |
       +-- Hybrid: Opaque token + session validation
       |
       +-- SemiHybrid: JWT validation
       |
       +-- PureJwt: JWT validation
       |
       v
Authenticated Identity
       |
       v
AccessContext
       |
       v
Authorization
       |
       v
Protected Operation
```

The authentication mode determines which validation steps are required.

The resulting identity must satisfy the same framework-level trust requirements regardless of the credential format.

An application must not construct a trusted identity directly from unvalidated client input.

## Authentication context and authorization context

UltimateAuth distinguishes the context used to execute an authentication flow from the context used to evaluate authorization.

### AuthFlowContext

`AuthFlowContext` represents the effective authentication context for an operation.

It includes the relevant mode, client characteristics, tenant context, and other applicable runtime information.

The framework establishes this context through controlled boundaries.

Once established, the effective context remains stable throughout the operation.

Application code and individual domain services must not independently replace or reconstruct it to change the security rules.

### AccessContext

`AccessContext` represents the trusted actor and authorization context after authentication has been evaluated.

It provides the identity and contextual information needed for authorization and policy decisions.

`AccessContext` does not authenticate credentials.

Authentication must be resolved before a trusted authorization context is established.

This separation allows the authentication mechanism to vary without requiring a different authorization architecture for each mode.

## Authentication lifecycle

Authentication does not end when credentials are issued.

Depending on the mode, an authenticated context may be continued, renewed, expired, or revoked.

UltimateAuth distinguishes several lifecycle operations.

### Authentication

Establishes a new authenticated context after the required proof has been validated.

### Refresh

Continues an existing authentication context when its continuation requirements are satisfied.

Refresh is mode-dependent.

PureOpaque uses session continuation and touch semantics rather than refresh-token rotation.

Hybrid and SemiHybrid use refresh-token rotation together with the applicable session validation requirements.

PureJwt may support session-independent token renewal when refresh tokens are enabled.

### Logout

Ends or discards the relevant authentication context according to the mode.

In session-authoritative modes, server-side session revocation can prevent subsequent authentication.

In JWT-based modes, removing credentials from the client does not by itself invalidate already issued JWTs.

### Revocation

Invalidates authentication state or credentials according to the guarantees supported by the selected mode.

Session revocation and JWT revocation are not equivalent operations.

A server-side session can be revoked immediately, but a JWT validated without session lookup can remain valid until expiration.

Applications requiring stronger access-token invalidation guarantees must select a mode and validation strategy that explicitly provides them.

## Authentication security boundaries

UltimateAuth's authentication architecture relies on several principles that apply across modes.

### Credentials are evidence, not authority

The presence of a token, cookie, or session identifier does not automatically establish identity.

The required validation must succeed first.

### Authentication modes have explicit guarantees

A mode must not silently weaken its validation or revocation guarantees because of client runtime, transport selection, or performance optimization.

### Session authority is mode-dependent

Server-side sessions are authoritative where the mode requires them.

Stateless authentication does not become session-backed merely because other parts of the framework support session management.

### Client context is not inherently trusted

Device identifiers, tenant identifiers, and client-provided runtime information must be handled according to the applicable trust boundaries.

They must not allow a client to bypass authentication requirements.

### Authentication and authorization remain separate

A valid identity does not automatically imply permission to access a resource.

Authorization policies must still evaluate the authenticated actor's access rights.

### Credential protection remains essential

Opaque session credentials, opaque access tokens, JWTs, and refresh tokens are all sensitive.

Their safe use depends on appropriate transport, storage, expiration, and validation policies.

## How the model relates to authentication modes

The authentication model establishes the shared principles and boundaries.

Authentication modes determine how those principles are executed.

| Mode | Authentication authority | Role of session state |
|---|---|---|
| PureOpaque | Server-side session | Required for authentication and lifecycle |
| Hybrid | Server-side session, accessed through an opaque token | Required for authentication and lifecycle |
| SemiHybrid | Cryptographically validated JWT for normal requests | Required for session lifecycle, not default request validation |
| PureJwt | Cryptographically validated JWT | Not required |

This distinction is essential to understanding UltimateAuth.

Root, Chain, and Session provide structured lifecycle control where server-side authentication state is required.

They are not universal prerequisites for authenticating a request.

Likewise, JWT authentication does not eliminate server authority. The server still defines which tokens are acceptable and which validation rules must be enforced.

## Summary

UltimateAuth defines authentication as a controlled process that establishes trusted identity from validated evidence.

Its architecture separates:

- Authentication proof and credential validation.
- Authentication state and lifecycle management.
- Authentication context and authorization context.
- Shared security boundaries and mode-specific execution strategies.

Root, Chain, and Session form the authentication lineage used by session-backed modes.

PureOpaque and Hybrid rely on that lineage as part of their authentication authority.

SemiHybrid retains session lifecycle management while allowing JWT authentication without request-time session lookup.

PureJwt does not require session persistence or validation.

These distinctions allow UltimateAuth to support different authentication strategies without changing the fundamental trust and authorization model.

## Next step

Continue to **Flow-Based Authentication** to understand how UltimateAuth establishes and executes authentication operations through its controlled flow pipeline.