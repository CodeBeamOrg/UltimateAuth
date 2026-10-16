---
title: Request Lifecycle
order: 7
group: fundamentals
---

# Request Lifecycle

UltimateAuth distinguishes between establishing authentication, validating an existing identity, and authorizing access to protected operations.

These responsibilities are related, but they do not necessarily execute through the same pipeline.

A login request may establish a new authentication context. A protected API request may only need to validate an existing credential. A server-side application operation may execute without making an additional HTTP request.

Understanding these differences is essential when integrating UltimateAuth into real applications.

This page focuses on **how authentication is evaluated during application requests and where the security boundaries are enforced**.

For authentication flow orchestration, see [Auth Flows](/docs/auth-flows/index).

## Two request categories

At a conceptual level, requests interacting with UltimateAuth fall into two categories.

| Category | Purpose | Typical examples |
|---|---|---|
| Authentication operations | Establish, continue, or terminate authentication | Login, refresh, logout |
| Protected resource operations | Access application functionality using an existing identity | API calls, protected pages, application commands |

The distinction matters because an authentication operation may change authentication state, while a protected resource operation generally consumes an existing authentication context.

Both require appropriate security checks, but they do not need to execute identical flows.

## Authentication operations

Authentication operations are explicitly initiated through supported authentication endpoints or application integrations.

Examples include login, refresh, and logout.

For HTTP authentication endpoints, UltimateAuth uses its flow infrastructure to resolve the effective authentication context and execute the requested operation.

Conceptually:

```text
Authentication Request
         |
         v
Authentication Flow Boundary
         |
         v
Effective AuthFlowContext
         |
         v
Operation-Specific Validation
         |
         v
Authentication Operation
         |
         v
Authentication Result
```

The operation may create or update sessions, issue credentials, rotate refresh tokens, revoke authentication state, or produce a result without persistent changes.

These details depend on the flow and authentication mode.

The internal execution pipeline is described in [Auth Flows](/docs/auth-flows/index), while individual operations are documented in their respective flow guides.

## Protected resource operations

Most application requests do not initiate a login, refresh, or logout flow.

Instead, they attempt to access a protected resource using an existing authentication credential or established server-side authentication context.

A typical protected HTTP request follows this conceptual lifecycle:

```text
Incoming Request
        |
        v
Resolve Applicable Authentication Scheme
        |
        v
Extract Authentication Credential
        |
        v
Validate Credential and Required State
        |
        v
Establish Trusted Identity
        |
        v
Evaluate Authorization
        |
        +---- Denied
        |
        v
Execute Protected Operation
        |
        v
Return Response
```

The exact framework components and ordering depend on the application integration.

However, the security distinction remains the same:

**Authentication establishes identity. Authorization determines whether that identity may perform the requested operation.**

A successfully authenticated request is not automatically authorized.

## Credential validation

The most important mode-dependent difference in the request lifecycle is how authentication credentials are validated.

UltimateAuth supports different authentication strategies.

### PureOpaque

The client presents an opaque session credential.

The server resolves the associated session and evaluates the applicable session, chain, and security requirements.

The session remains authoritative for authentication.

A revoked or expired session must not continue to authenticate subsequent requests.

### Hybrid

The client presents an opaque access token.

The server resolves the token and validates its relationship to the authoritative server-side session.

The access token and session are separate concepts.

Authentication requires both a valid access credential and the applicable session validation requirements to succeed.

### SemiHybrid

The client presents a JWT access token.

Normal resource authentication validates the JWT cryptographically and applies the relevant token-validation rules.

A server-side session remains relevant for lifecycle operations such as refresh and session management, but normal JWT validation does not require a session lookup.

Revoking a session therefore does not automatically invalidate an already issued JWT.

### PureJwt

The client presents a JWT access token.

The resource validates the token cryptographically and applies the configured issuer, audience, lifetime, and other relevant validation requirements.

Authentication does not require a server-side session.

Previously issued JWTs may remain valid until expiration unless an additional invalidation mechanism is explicitly configured.

For the complete guarantees and tradeoffs, see [Authentication Modes](/docs/fundamentals/auth-modes).

## When identity becomes trusted

An incoming request may contain user identifiers, claims, tokens, cookies, device information, or tenant-related metadata.

These values must not automatically be treated as a trusted identity.

The relevant authentication mechanism must validate the presented evidence before the application relies on it.

In session-authoritative modes, this includes the required server-side session checks.

In JWT-based modes, it includes the applicable cryptographic and token-validation checks.

Only after successful authentication should the application treat the resulting identity as authenticated.

A failed authentication attempt must not leave behind an identity that appears successfully authenticated to downstream application code.

### Anonymous requests

Not every application request requires authentication.

Public endpoints may allow anonymous access.

A request without valid authentication credentials can remain anonymous when the endpoint permits it.

For protected endpoints, missing or invalid authentication must prevent access according to the applicable authentication and authorization configuration.

An anonymous request is not necessarily an error. It becomes an access problem when the requested operation requires an authenticated identity.

## Authentication context and AccessContext

UltimateAuth separates authentication-flow execution from authorization context.

`AuthFlowContext` is associated with authentication operations and describes the effective environment in which those operations execute.

It includes the applicable mode, client profile, tenant, and other flow-related information.

`AccessContext` represents the trusted actor and contextual information used for access and authorization decisions.

These contexts serve different purposes.

An application request does not need to execute a login flow merely to establish an identity from an already valid authentication credential.

Likewise, the existence of an `AuthFlowContext` does not mean the operation has already been authorized.

The authentication model and these trust boundaries are introduced in [Authentication Model](/docs/fundamentals/auth-model).

## Authorization boundary

After authentication establishes the caller's identity, the application must determine whether the requested operation is permitted.

Authorization may depend on:

- The authenticated actor.
- The requested resource or operation.
- Tenant boundaries.
- Roles and permissions.
- Applicable application policies.
- Resource ownership or other domain-specific requirements.

UltimateAuth integrates with the relevant server-side authorization mechanisms, but authentication and authorization remain separate responsibilities.

For ASP.NET Core applications, endpoint and policy-based authorization can protect HTTP resources.

However, an authorization attribute applied to an HTTP endpoint does not automatically protect arbitrary direct calls to the underlying application service.

Security requirements must be enforced at the boundary through which the operation is actually accessible.

## HTTP requests and in-process operations

Not every protected application operation crosses an HTTP boundary.

This distinction is especially important for server-hosted applications.

### HTTP resource requests

A remote client sends an HTTP request to a protected application or resource API.

The receiving application must authenticate the request and enforce authorization before performing the protected operation.

Client-side authentication state alone is insufficient.

### In-process application operations

A server-hosted application may invoke application services directly without making an HTTP request.

For example, a Blazor Server component may call a registered service within the same process.

In this case, the application must not assume that HTTP middleware or endpoint authorization filters execute automatically.

The service invocation must operate with an appropriately established identity and enforce the relevant authorization requirements.

This is an architectural distinction, not a separate authentication mode.

See [Runtime Architecture](/docs/fundamentals/runtime-architecture) for integrated and distributed deployment models.

## Long-lived connections

Some application environments maintain connections or execution contexts that outlive individual HTTP requests.

Blazor Server is an important example.

An interactive circuit may remain active after its initial authentication state was established.

During that time, the underlying authentication state may change.

A session may expire, be revoked, or become invalid because of a security-sensitive operation.

Consequently, applications must account for authentication-state revalidation and authorization during long-lived interactions.

An initially authenticated connection must not be treated as permanent proof that every later operation remains authorized.

The appropriate mechanism depends on the runtime integration and authentication mode.

## Authentication failures and authorization failures

Authentication and authorization failures represent different conditions.

| Condition | Meaning | Typical HTTP outcome |
|---|---|---|
| Missing required authentication | No acceptable authenticated identity was established | 401 Unauthorized |
| Invalid or expired credential | Presented evidence failed authentication | 401 Unauthorized |
| Authenticated but insufficient permission | Identity is valid, but access is denied | 403 Forbidden |
| Public endpoint accessed anonymously | Authentication is not required | Request proceeds |

These are typical HTTP semantics, not guarantees that every integration returns identical status codes.

Browser applications may use redirects or other configured challenge behavior.

Application services operating outside HTTP may express failures through typed results or exceptions rather than HTTP responses.

The essential distinction is that authentication failure and authorization denial must not be confused.

## Request isolation

Authentication state must be associated with the correct execution context.

A request must not inherit another request's authenticated identity, tenant context, or authorization state.

This matters in multi-tenant systems, applications with concurrent requests, and server-side environments where services may have different lifetimes.

Request-specific context must be resolved and propagated through the appropriate framework boundaries.

Long-lived or singleton services must not accidentally retain request-specific authentication information.

The same principle applies to direct application service calls and background operations.

A background operation without an authenticated HTTP request must establish its execution and authorization context through an explicitly supported mechanism rather than assuming a user identity exists.

## What the request lifecycle does not guarantee

The request lifecycle should not be interpreted as a promise that every UltimateAuth operation:

- Creates an `AuthFlowContext`.
- Resolves a server-side session.
- Queries authentication persistence.
- Rotates or issues tokens.
- Executes the same orchestrator or authority method.
- Passes through an HTTP endpoint.
- Uses identical authorization mechanisms.

Those requirements depend on the operation, authentication mode, and runtime integration.

The shared guarantee is architectural: authentication evidence must be validated according to the applicable security model, and protected operations must enforce their authorization requirements.

## Summary

UltimateAuth distinguishes between authentication operations and requests that consume an existing authenticated identity.

Authentication operations establish, continue, or terminate authentication through controlled flows.

Protected resource operations validate existing authentication evidence, establish trusted identity, and enforce authorization before allowing access.

The validation strategy depends on the authentication mode.

The execution boundary depends on the runtime and deployment architecture.

**A request is not trusted because it contains a credential. It becomes authenticated only when the required validation succeeds, and it becomes authorized only when the applicable access rules permit the operation.**

## Next step

You have completed the Fundamentals section.

Continue to [Auth Flows](/docs/auth-flows/index) to explore the technical authentication pipeline and the individual login, refresh, and logout operations.