---
title: Authentication Modes
order: 4
group: fundamentals
---

# Auth Modes

UltimateAuth provides four authentication modes: **PureOpaque**, **Hybrid**, **SemiHybrid**, and **PureJwt**.

Each mode defines how authentication credentials are issued, validated, renewed, and revoked. The modes share the same authentication and authorization architecture, but differ in their reliance on server-side sessions, credential formats, and lifecycle guarantees.

The choice of authentication mode affects more than performance. It determines where authentication state is maintained, whether a server-side session must be consulted during an authenticated request, and how quickly previously issued credentials can be invalidated.

> **Availability**
>
> PureOpaque and Hybrid are supported in the initial release. SemiHybrid and PureJwt are defined by the UltimateAuth architecture but are not yet available as completed, production-supported modes. Their behavior is documented here to explain the intended authentication model.

## Auth credentials

UltimateAuth distinguishes three credential concepts.

**Session credential**

A session credential identifies an authenticated session maintained by the server. In PureOpaque, it is the credential presented by the client to authenticate requests.

A session credential may be represented by an opaque, randomly generated value. It does not need to expose the internal database identifier of the session.

**Access token**

An access token is a credential used to authenticate requests to protected resources.

Depending on the mode, it may be:

- An opaque token resolved and validated by the server.
- A JWT validated cryptographically without a session lookup.

An access token is not necessarily a session identifier. In Hybrid, the opaque access token is a separate credential associated with a server-side session.

**Refresh token**

A refresh token allows an existing authentication context to continue by obtaining replacement credentials without requiring the user to authenticate again.

Refresh tokens have their own validation, expiration, rotation, and replay-protection requirements.

Not every authentication mode requires all three credential types.

## Mode comparison

| Characteristic | PureOpaque | Hybrid | SemiHybrid | PureJwt |
|---|---|---|---|---|
| Client authentication credential | Opaque session credential | Opaque access token | JWT access token | JWT access token |
| Separate access token | No | Yes | Yes | Yes |
| Access token format | Not applicable | Opaque | JWT | JWT |
| Refresh token | No | Yes | Yes | Optional |
| Server-side session | Required | Required | Required | Not required |
| Session lookup on authenticated requests | Required | Required | Not required by default | Not required |
| Session role | Authentication authority | Authentication authority | Lifecycle management | None |
| Access token validation | Session validation | Token and session validation | Cryptographic JWT validation | Cryptographic JWT validation |
| Session continuation | Session touch | Refresh token rotation and session policies | Refresh token rotation with session validation | Independent token renewal, if enabled |
| Immediate session revocation | Yes | Yes | Prevents continuation; does not automatically invalidate issued JWTs | Not applicable |
| Immediate access-token revocation | Through session invalidation | Through session invalidation | Requires an additional revocation check | Not supported by the pure stateless model |
| Device/session management | Yes | Yes | Yes | No session-based device management |
| Server-side state for authentication | Required | Required | Required for lifecycle, not normal JWT validation | Not required |
| Main advantage | Simple, centrally controlled sessions | Separate token lifecycle with session authority | Stateless access validation with session management | Minimal authentication-state dependencies |
| Main trade-off | Session lookup on each authenticated request | More credentials and lifecycle complexity | Existing JWTs can remain valid after session revocation | No immediate server-side session revocation |

The comparison describes authentication behavior, not guaranteed throughput rankings. Actual performance depends on token validation costs, storage infrastructure, caching, network latency, request volume, and application architecture.

## PureOpaque

### Fully session-based authentication

PureOpaque is UltimateAuth's simplest stateful authentication mode.

Authentication is represented by a server-side session. The client receives an opaque session credential and presents it on subsequent authenticated requests.

The server resolves the credential, validates the session and its applicable security policies, and establishes the authenticated identity.

```text
Client
  |
  | Session Credential
  v
Session Resolution
  |
  v
Session Validation
  |
  v
Authenticated Identity
  |
  v
AccessContext
```

PureOpaque does not issue a separate access token or refresh token.

The session credential itself is sufficient to identify the authentication context, provided the server determines that the session remains valid.

### Session continuation

PureOpaque does not use refresh-token rotation.

Instead, an existing session may continue through session touch operations, subject to the configured session policies.

Depending on the configuration, these policies may include:

- Session lifetime.
- Sliding expiration.
- Idle timeout.
- Touch interval.
- Absolute expiration limits.
- Session revocation.

A touch operation does not necessarily extend the session lifetime. It updates session state only when the applicable policy requires it.

An expired or revoked session cannot be restored merely by touching it.

### Advantages

**Minimal credential complexity**

There is no separate access-token lifecycle, refresh-token storage, or refresh-token rotation.

**Immediate session revocation**

When a session is revoked, subsequent authentication evaluations that validate that session reject the credential.

**Centralized authentication control**

The server remains authoritative for session validity and can enforce current session policies.

**Natural integration with server-rendered applications**

Applications already maintaining server-side authentication state can use a single session credential without introducing additional token-management mechanisms.

### Limitations

**Session validation is required**

Every authenticated request must validate the server-side session. Depending on the deployment, this can involve a database or distributed cache lookup.

**Availability depends on session infrastructure**

A distributed deployment needs reliable access to shared session state or an equivalent session-validation mechanism.

**The session credential is sensitive**

Possession of a valid session credential may be sufficient to authenticate as the session owner. It must be protected against disclosure, replay, and session fixation.

**Long-lived connections require special handling**

In environments such as Blazor Server, an established SignalR circuit may outlive the authentication state initially used to establish it. Appropriate revalidation and authorization checks remain necessary.

### Recommended applications

PureOpaque is generally a strong default for:

- Blazor Server applications.
- ASP.NET Core MVC and Razor Pages applications.
- Internal applications using centrally managed sessions.
- Browser applications using secure, HttpOnly session cookies.
- Systems that prioritize simple session lifecycle management and immediate server-side revocation.

PureOpaque is not restricted to server-to-server communication. It can also be used safely with browser and mobile clients when credentials are stored, transported, and validated appropriately.

## Hybrid

### Opaque access tokens with authoritative sessions

Hybrid combines separate access and refresh tokens with mandatory server-side session validation.

Unlike PureOpaque, the client does not use the session credential as its primary authentication credential.

Instead, the client receives an **opaque access token** associated with a server-side session.

The access token contains no readable JWT claims. The server resolves the token and validates the associated session before establishing the authenticated identity.

```text
Client
  |
  | Opaque Access Token
  v
Access Token Resolution
  |
  v
Associated Session
  |
  v
Session Validation
  |
  v
Authenticated Identity
  |
  v
AccessContext
```

The opaque access token and internal session identifier represent different concepts.

The access token is a client-facing authentication credential with its own lifetime. The session represents the server-authoritative authentication state.

### Refresh and session lifecycle

Hybrid uses refresh tokens to continue authentication without requiring a new login.

A successful refresh validates the relevant session and refresh-token state, applies the configured session policies, and issues replacement credentials.

Access-token lifetime and session lifetime can therefore be managed separately.

For example, an application may use a short-lived opaque access token while maintaining a longer-lived session, subject to refresh-token expiration and revocation rules.

A valid refresh token must not be allowed to restore an expired or revoked session.

### Advantages

**Separate credential lifetimes**

Access tokens can expire independently of the underlying session.

**Immediate session-based revocation**

Because session validity is enforced during authentication, revoking the session prevents associated access tokens from authenticating on subsequent validations.

**Opaque access credentials**

The access token does not disclose identity claims, roles, tenant information, or other application data through a readable token payload.

**Centralized security policies**

Session status and related security conditions can be enforced as part of authentication.

**Suitable for API-oriented clients**

Applications can use access and refresh tokens while retaining server-side control over the authenticated session.

### Limitations

**More complex lifecycle**

Hybrid requires access-token issuance, refresh-token management, rotation, replay protection, and session lifecycle coordination.

**Server-side validation remains mandatory**

Opaque access tokens do not provide stateless JWT validation. The server must resolve the token and enforce the associated session state.

**Additional infrastructure dependencies**

Token resolution and session validation may require storage or cache access.

**Credential theft remains a risk**

Opaque tokens conceal claims but remain bearer credentials. Their format does not prevent unauthorized use if they are stolen.

### Recommended applications

Hybrid is generally appropriate for:

- Blazor WebAssembly applications communicating with an API.
- Web applications with a separate backend API.
- Mobile and desktop applications requiring refresh-token continuity.
- Full-stack applications needing centrally controlled authentication.
- Systems requiring separate access-token expiration and immediate session revocation.

Hybrid is particularly useful when the application benefits from access/refresh token semantics but does not want to sacrifice server-authoritative session validation.

## SemiHybrid

### Stateless JWT validation with server-side session lifecycle

SemiHybrid combines JWT access tokens with server-side session management.

Its defining characteristic is the separation between **access-token validation** and **session lifecycle operations**.

For normal authenticated requests, the server validates the JWT cryptographically. A session lookup is not required by default.

```text
Client
  |
  | JWT Access Token
  v
JWT Validation
  |
  v
Authenticated Identity
  |
  v
AccessContext
```

The server-side session is not consulted during this normal authentication path.

However, the session remains relevant when the authentication lifecycle must be managed.

### What session metadata is used for

SemiHybrid maintains server-side session state for operations such as:

- Refresh-token validation and rotation.
- Logout and session termination.
- Revoking an individual session.
- Revoking all sessions belonging to a user.
- Listing active sessions or devices.
- Enforcing session expiration and continuation policies.
- Recording session lifecycle information.

This state is a **lifecycle control mechanism**, not a mandatory source of identity validation on every API request.

For example, when a user logs out, the server can revoke the session and prevent subsequent refresh operations from continuing it.

However, a previously issued JWT may remain valid until its expiration because normal JWT validation does not consult that session.

### Revocation semantics

SemiHybrid must distinguish two separate guarantees.

**Session revocation**

Revoking the session prevents authentication continuation through that session, including refresh operations that require it.

**Access-token revocation**

Revoking the session does not automatically invalidate an already issued JWT.

Immediate rejection of an issued JWT requires an additional mechanism, such as a revocation lookup against a distributed cache.

Such a mechanism can be supported as an explicit policy, but it introduces an additional request-time dependency and validation cost.

Without that policy, JWT expiration remains the boundary for an already issued access token.

### Advantages

**Stateless normal request validation**

Protected API requests can authenticate JWTs without consulting the session store.

**Centralized lifecycle management**

Applications retain the ability to manage sessions, devices, refresh operations, and future authentication continuity.

**Reduced request-time storage dependencies**

Session infrastructure is not part of the default JWT validation path.

**Configurable revocation trade-offs**

Applications can choose whether to introduce additional request-time revocation enforcement when stronger invalidation guarantees are required.

### Limitations

**Session revocation does not automatically revoke issued JWTs**

An access token can remain valid after logout until its expiration unless an additional revocation mechanism is enforced.

**Lifecycle state is still required**

SemiHybrid is not a session-free mode. Refresh and session management depend on server-side state.

**More complex expiration semantics**

JWT expiration, refresh-token expiration, session expiration, and session activity policies must be coordinated.

**Optional revocation checks reduce statelessness**

If immediate access-token revocation is enabled, the request pipeline may need an additional cache or store lookup.

### Recommended applications

SemiHybrid is intended for:

- High-throughput APIs that still require centralized session management.
- Distributed applications using short-lived JWT access tokens.
- Applications offering device and active-session management.
- Systems that need JWT validation performance without giving up refresh and session lifecycle control.
- Architectures where access-token expiration is an acceptable default revocation boundary.

SemiHybrid should not be described as providing immediate JWT revocation unless an additional enforcement mechanism is enabled.

## PureJwt

### Session-independent JWT authentication

PureJwt is UltimateAuth's fully stateless JWT authentication mode.

It does not require server-side session creation, persistence, resolution, or validation.

The client receives a JWT access token. Protected resources validate the token using its signature and applicable validation rules, including issuer, audience, expiration, and other security-relevant claims.

```text
Client
  |
  | JWT Access Token
  v
Cryptographic JWT Validation
  |
  v
Authenticated Identity
  |
  v
AccessContext
```

No session lookup is involved.

PureJwt must not require session infrastructure indirectly through token issuance, validation, or other authentication pipeline components.

### Token renewal

PureJwt can operate without refresh tokens.

When refresh tokens are enabled, their lifecycle must remain independent of server-side sessions.

Secure refresh-token rotation and replay protection may require dedicated refresh-token state. This does not imply a dependency on the session model.

The distinction is important:

- **Session independence** means that authentication does not require server-side sessions.
- **Stateless access validation** means that protected requests do not require server-side token or session lookups.
- **Refresh-token persistence**, when enabled, is a separate lifecycle concern.

An application seeking a completely stateless credential lifecycle can disable refresh tokens and require reauthentication when access tokens expire.

### Revocation semantics

PureJwt does not provide session-based immediate revocation.

A correctly signed, unexpired JWT remains valid under the configured validation rules unless a relevant validation condition changes, such as the availability of its signing key.

PureJwt does not maintain a mandatory blacklist or server-side session revocation mechanism.

Applications requiring immediate per-session access revocation should consider Hybrid, or SemiHybrid with an explicit revocation enforcement strategy.

### Advantages

**No mandatory session infrastructure**

Authentication does not depend on session stores or session-management services.

**Independent resource validation**

Services with the necessary verification keys and validation configuration can validate access tokens locally.

**Suitable for distributed architectures**

Protected services do not need to contact a central authentication session store for each request.

**Reduced lifecycle complexity when refresh is disabled**

Applications can avoid session management and refresh-token rotation entirely.

**Predictable validation behavior**

Authentication is determined by the token and the applicable cryptographic and claim-validation rules.

### Limitations

**No immediate per-session revocation**

An issued JWT can remain usable until expiration.

**Token lifetime becomes particularly important**

Long-lived access tokens increase the period during which a stolen token may remain usable.

**Claims can become stale**

Identity or authorization-related claims embedded in a JWT do not automatically change when server-side user data changes.

**JWT payloads are readable**

Signed JWTs are not encrypted by default. Sensitive information should not be placed in their payloads.

**Key management is critical**

Signing-key protection, key rotation, issuer and audience validation, and accepted algorithm configuration are essential.

### Recommended applications

PureJwt is intended for:

- Stateless resource APIs.
- Distributed services and microservices.
- External API integrations.
- Workloads requiring independent JWT validation.
- Systems that do not require centralized session management.
- Applications prioritizing minimal authentication-state dependencies.

PureJwt is not automatically the fastest mode in every deployment. Cryptographic validation has a cost, while well-cached stateful authentication can also be efficient. Its primary architectural advantage is eliminating mandatory session dependencies.

## Choosing a mode

The most useful selection criterion is not the client technology alone, but the authentication guarantees the application requires.

| Application or requirement | Recommended mode | Reason |
|---|---|---|
| Blazor Server | PureOpaque | Server-controlled sessions without a separate access/refresh token lifecycle |
| ASP.NET Core MVC or Razor Pages | PureOpaque | Simple cookie-based session authentication |
| Internal administrative application | PureOpaque or Hybrid | Centralized session control and immediate revocation |
| Blazor WebAssembly with a separate API | Hybrid | Access/refresh tokens with authoritative session validation |
| Mobile or desktop client | Hybrid | Independent token lifetimes with centralized session control |
| Web application with separate frontend and API | Hybrid | Token-based access without losing server-side revocation |
| High-throughput API with session management | SemiHybrid | Stateless JWT validation alongside managed session lifecycle |
| Multi-device application with session controls | SemiHybrid or Hybrid | Centralized device/session management; different access-validation costs |
| Distributed resource APIs | PureJwt | Independent JWT validation without mandatory session lookup |
| External service integration | PureJwt | Stateless token validation across service boundaries |
| Immediate access revocation is essential | PureOpaque or Hybrid | Every authentication evaluation enforces server-side session validity |
| No session infrastructure is desired | PureJwt | Authentication does not require session persistence |

These are recommendations, not restrictions.

For example, Blazor WebAssembly can use PureOpaque through a secure cookie-based backend-for-frontend architecture. Similarly, a server-rendered application can use JWT authentication when its broader system requirements justify it.

The runtime does not inherently determine the security model.

## Client Profiles and authentication modes

UltimateAuth supports different client environments through Client Profiles.

A Client Profile can provide suitable defaults for a runtime, including credential delivery and authentication-mode selection.

However, a Client Profile and an authentication mode represent different concerns.

**Client Profile**

Describes how the application interacts with the authentication system, including runtime characteristics and credential transport.

**Authentication Mode**

Defines the authentication credential model, session dependencies, validation strategy, and lifecycle guarantees.

A Blazor Server profile may default to PureOpaque, while a Blazor WebAssembly profile may default to Hybrid.

These defaults do not mean the modes are exclusive to those environments.

Applications can override the effective mode through supported configuration and context-resolution mechanisms.

When different clients share the same UAuthHub, the server must resolve the effective mode through trusted configuration and framework-controlled context. An untrusted client must not be allowed to select a weaker authentication policy arbitrarily.

The effective mode must remain stable throughout a single authentication operation.

## Security considerations

All four modes can support secure applications when implemented and configured correctly. None should be described as universally more secure than the others.

Their security guarantees differ.

### Credential confidentiality

Opaque credentials do not expose readable claims, but possession of a valid bearer credential may still permit authentication.

JWT signatures protect integrity and authenticity; they do not conceal the payload.

All credentials require appropriate transport and storage protections.

### Revocation guarantees

PureOpaque and Hybrid enforce server-side session validity during authenticated requests.

SemiHybrid enforces session validity during lifecycle operations but does not require it during normal JWT validation.

PureJwt does not depend on sessions and does not provide session-based revocation.

These differences must be considered when selecting token lifetimes and logout behavior.

### Browser security

When authentication credentials are delivered through cookies, appropriate `HttpOnly`, `Secure`, and `SameSite` settings should be applied.

Cookie-based authentication also requires appropriate CSRF protections.

When tokens are accessible to browser application code, XSS and token-exfiltration risks require particular attention.

The authentication mode does not eliminate these browser security requirements.

### Authorization remains separate

Authentication establishes the identity of the current actor.

Authorization determines what that actor is permitted to do.

All modes must produce trusted authentication state before authorization policies are evaluated.

Choosing a stateless mode does not remove the need for authorization checks or appropriate handling of stale authorization claims.

## Summary

UltimateAuth's four authentication modes represent four distinct strategies:

**PureOpaque** uses a server-side session as the authentication authority and avoids a separate access/refresh token lifecycle.

**Hybrid** uses separate opaque access and refresh tokens while requiring authoritative session validation for authenticated requests.

**SemiHybrid** uses JWT access tokens for stateless request authentication while retaining server-side sessions for refresh, logout, revocation, and device management.

**PureJwt** uses JWT access tokens without requiring server-side session infrastructure.

The correct mode is the one whose validation, lifecycle, revocation, and operational guarantees match the application's requirements.

UltimateAuth preserves a consistent authentication and authorization architecture while allowing these underlying strategies to differ.