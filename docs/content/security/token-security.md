---
title: Token Security
order: 2
group: security
---

# Token Security

Authentication tokens allow clients to present authentication state to protected applications and services.

However, not all tokens provide the same security guarantees.

An opaque credential requires authoritative server-side validation. A JSON Web Token (JWT) can be verified locally using its signature and claims. A refresh credential enables authentication to continue without repeating the original login process.

UltimateAuth supports these different mechanisms through its authentication modes.

**Token security depends on the credential's purpose, how it is validated, how long it remains valid, and which security state is consulted during validation.**

This page explains the security characteristics of access and refresh credentials, including issuance, expiration, rotation, replay risks, and revocation boundaries.

For the overall authentication architecture, see [Authentication Modes](/docs/fundamentals/authentication-modes).

## Authentication State and Credentials

A token is authentication material presented by a client.

It is not the user identity itself.

Depending on the authentication mode, the token may reference server-managed authentication state or contain signed claims that can be validated without retrieving a session.

These approaches establish different trust boundaries.

### Server-Authoritative Authentication

In server-authoritative modes, a presented opaque credential is validated against server-managed authentication state.

The server can evaluate the current validity of the corresponding session and applicable security conditions.

This allows revocation and other authoritative state changes to affect subsequent protected requests.

### Locally Validated Authentication

In JWT-based modes, a protected resource can validate a token using its signature, issuer, audience, expiration, and other applicable token requirements.

The resource may not need to consult the issuing server or a session store for every request.

This reduces the dependency on server-side session lookups but changes how quickly revocation and authorization changes can be enforced.

**Neither model makes the token an identity record. The token is a credential whose validity is determined by the selected authentication mechanism.**

## Token Types

UltimateAuth uses two principal access credential formats: opaque credentials and JWTs.

Refresh credentials serve a separate purpose.

| Credential | Purpose | Validation model |
|---|---|---|
| **Opaque access credential** | Authenticate protected requests | Authoritative server-side validation |
| **JWT access token** | Authenticate protected requests | Cryptographic and claims validation |
| **Opaque refresh credential** | Continue authentication and obtain renewed credentials | Refresh-specific validation and state checks |

The distinction between access and refresh credentials is important.

Access credentials are presented to protected resources.

Refresh credentials are presented to the authentication system for a continuation operation. They should not be accepted as ordinary access credentials.

## Opaque Access Credentials

An opaque credential is a value whose contents do not expose the authenticated identity or authorization claims to the client.

Conceptually:

```text
Access credential:
q8Vn2...random-opaque-value...M7x
```

The client presents the credential, but cannot determine its authorization meaning by decoding it.

The server resolves and validates the associated authentication state.

### Security Properties

Opaque access credentials provide several useful characteristics:

- Their meaning is determined by authoritative server-side state.
- They do not need to expose identity claims in a client-readable payload.
- Their validity can be affected by session expiration or revocation.
- They can participate in account-level and chain-level security enforcement.

However, opaque credentials are still bearer credentials unless an additional proof mechanism is enforced.

An attacker who obtains a usable opaque credential may be able to present it.

Opacity alone does not prevent credential theft or replay.

### Server-Side Validation

In a session-authoritative mode, validation can enforce the applicable session hierarchy requirements, including expiration, revocation, ownership, and security state consistency.

See [Session Security Model](/docs/security/session-security-model).

## JWT Access Tokens

A JWT is a structured token containing signed claims.

A typical JWT contains three encoded sections:

```text
header.payload.signature
```

Its payload may describe the authenticated subject, tenant, authorization information, token lifetime, and other relevant context.

A JWT is not encrypted merely because it is encoded and signed.

Applications should not place secrets or unnecessarily sensitive information in a JWT payload.

### Cryptographic Validation

A protected resource must validate the JWT according to the configured token requirements.

Relevant checks include:

- Signature validity.
- Accepted signing algorithm and key.
- Expected issuer.
- Intended audience.
- Expiration and other applicable time constraints.
- Required token claims and application-specific restrictions.

A successfully decoded JWT is not necessarily a valid JWT.

Likewise, a correctly signed JWT is not automatically acceptable to every resource.

The resource must verify that the token was issued for its intended security context.

### Local Validation

JWTs can be validated without loading current server-side session state.

This is useful for distributed APIs and services that need to authenticate requests efficiently.

However, local validation introduces an important limitation:

**A token can remain cryptographically valid after the underlying account or authorization state has changed.**

Whether such changes immediately affect access depends on additional validation and revocation mechanisms.

## Authentication Mode Behavior

UltimateAuth selects credential behavior according to the configured authentication mode.

| Mode | Access mechanism | Continuation mechanism | Protected request authority |
|---|---|---|---|
| **PureOpaque** | Opaque session credential | Session lifecycle | Server-managed session state |
| **Hybrid** | Opaque access credential | Opaque refresh credential | Server-managed session state |
| **SemiHybrid** | JWT access token | Server-managed refresh flow | Locally validated JWT |
| **PureJwt** | JWT access token | Optional refresh mechanism | Locally validated JWT |

### PureOpaque

PureOpaque uses an opaque session credential.

It does not require a separate access-token and refresh-token pair.

The credential is validated through the authoritative session mechanism.

Session lifecycle operations govern the continuation and validity of authentication state.

### Hybrid

Hybrid uses distinct opaque access and refresh credentials.

Access requests are validated against authoritative server-managed state.

Refresh operations use the refresh credential to continue authentication according to the configured lifecycle and rotation behavior.

**Hybrid does not require JWT access tokens.**

### SemiHybrid

SemiHybrid issues JWT access tokens while maintaining server-side session state for relevant lifecycle operations.

Protected requests can validate JWT access tokens locally.

Refresh and logout interact with the server-managed authentication lifecycle.

This means that server-side revocation does not necessarily invalidate an already-issued JWT immediately.

### PureJwt

PureJwt uses JWT access tokens without requiring the Root → Chain → Session hierarchy for ordinary protected request validation.

An optional refresh mechanism may maintain refresh-specific state.

The existence of a refresh store does not make JWT access validation session-authoritative.

For more detail, see [Authentication Modes](/docs/fundamentals/authentication-modes).

## Access Token Lifetime

Access credentials should have lifetimes appropriate to their authentication mode and security requirements.

Short-lived JWT access tokens are particularly important because a token may remain usable until expiration even after server-side authorization state changes.

For example, an application might choose a JWT access lifetime of several minutes.

However, **a lifetime such as 5–15 minutes is an example of an application security choice, not a universal UltimateAuth default or requirement**.

The appropriate value depends on factors such as:

- Sensitivity of protected resources.
- Expected frequency of authorization changes.
- Acceptable revocation delay.
- Availability and security of the refresh mechanism.
- Client behavior and network conditions.

### Lifetime Versus Revocation

Expiration limits how long a credential can be accepted.

Revocation invalidates authentication state before its ordinary expiration.

These mechanisms are related but not interchangeable.

In server-authoritative modes, current revocation state can be checked during protected requests.

In locally validated JWT modes, expiration may remain the primary limit on an already-issued access token unless an additional revocation check is implemented.

## Refresh Credentials

Refresh credentials allow clients to continue authentication without repeating the original authentication ceremony.

They are intended for the authentication system's refresh operation, not for general API access.

In UltimateAuth, refresh behavior is mode-dependent.

PureOpaque does not require a separate refresh credential pair, while Hybrid and SemiHybrid use refresh flows. PureJwt can support an optional refresh mechanism.

### Refresh Security Requirements

A secure refresh operation must establish that the presented credential is eligible for continued authentication.

Depending on the mode and configured strategy, this may involve checking:

- Credential existence and validity.
- Expiration.
- Revocation or consumption state.
- Associated user or session context.
- Applicable client and device context.
- Rotation and reuse conditions.

A valid refresh credential should not automatically bypass account-level or session-level security requirements that apply to the selected mode.

### Access and Refresh Separation

Access and refresh credentials have different responsibilities.

```text
Authentication
      |
      v
Access credential
      |
      | Used for protected requests
      v
Access expires
      |
      v
Refresh operation
      |
      | Validate refresh credential
      | Apply continuation rules
      v
Renewed access credential
```

The refresh operation does not extend the expiration timestamp of an already-issued JWT.

It issues new authentication material when continuation is permitted.

## Refresh Rotation

Refresh rotation replaces refresh authentication material according to the configured refresh strategy.

A rotating refresh mechanism can invalidate the previously used refresh credential and issue a replacement.

Conceptually:

```text
Refresh credential A
        |
        | Successful refresh
        v
Refresh credential A consumed
Refresh credential B issued
```

This reduces the period during which an older refresh credential remains reusable.

However, **rotation behavior is not identical across every UltimateAuth mode or refresh strategy**.

Some session continuation operations may update existing server-side state without replacing a separate refresh credential.

The effective behavior must follow the selected mode and its configured refresh strategy.

### Rotation Is Not Replay Prevention by Itself

Rotation limits credential reuse only when the server enforces the relevant consumption or replacement state.

A secure rotating refresh mechanism must account for situations such as:

- The same credential being submitted again.
- Concurrent refresh requests.
- A credential being reused after replacement.
- An interrupted response after a successful rotation.
- An attacker and legitimate client attempting to use the same credential.

The exact outcome depends on the implemented refresh strategy and concurrency guarantees.

Rotation should not be described as automatically eliminating every replay risk.

## Refresh Token Reuse

Refresh token reuse occurs when a credential is presented after it has already been consumed, replaced, or otherwise invalidated.

For a single-use rotating refresh credential, this is a security-relevant event.

Conceptually:

```text
Client refreshes with A
        |
        v
A replaced by B
        |
        v
Another request presents A
        |
        v
Reuse / invalid credential condition
```

A reuse attempt may indicate credential theft, but it can also arise from concurrent requests or client synchronization failures.

The refresh implementation must distinguish these situations according to its supported guarantees and policy.

### Security Response

Possible security responses include:

- Rejecting the invalid refresh attempt.
- Invalidating the associated refresh state.
- Revoking an affected session.
- Revoking a broader session chain where supported and appropriate.

These are distinct actions.

The presence of a reuse-detection mechanism does not imply that every reuse attempt automatically revokes the entire device-associated chain.

Such behavior must be explicitly supported by the configured refresh strategy.

## Refresh Credential Storage

Refresh credentials are generally more sensitive to long-term compromise because they may allow new access credentials to be obtained.

Their storage therefore deserves stronger protection than ordinary client-visible access metadata.

### Server-Side Storage

For persisted opaque refresh credentials, storing a one-way verifier or hash instead of the raw credential can reduce the impact of a storage disclosure.

However, the exact persistence model depends on the selected authentication mode and refresh implementation.

It should not be assumed that every authentication mode uses the same refresh storage structure.

### Client-Side Storage

The appropriate client storage mechanism depends on the application environment.

For browser applications, HttpOnly cookies can prevent JavaScript from directly reading refresh credentials, although cookie-based refresh flows still require appropriate CSRF and origin protections.

Native applications may use platform-provided secure storage where suitable.

Refresh credentials should not be exposed through application logs, diagnostic output, URLs, or unrelated API responses.

### Transport Security

Authentication credentials must be protected in transit.

Production authentication and refresh endpoints should use HTTPS.

Applications should also avoid sending refresh credentials to services that do not need to receive them.

## Claims and Authorization Freshness

JWT claims represent information established when the token was issued.

They may include standard or application-specific information such as:

```text
sub       Subject identifier
iss       Issuer
aud       Audience
exp       Expiration
iat       Issued-at time
jti       Token identifier
```

Depending on the application, a JWT may also include tenant information, roles, permissions, or session references.

These claims are not automatically updated when the underlying account changes.

### Example: Permission Removal

Consider the following sequence:

1. A JWT is issued containing an administrative permission.
2. An administrator removes that permission from the user.
3. The client continues presenting the previously issued JWT.

If the protected resource relies only on the signed claims in that JWT, the removed permission may remain effective until the token expires.

The signature remains valid because the token has not been modified.

### Authorization Strategies

Applications can address this limitation through their security architecture, including:

- Shorter JWT lifetimes.
- Reissuing claims during refresh.
- Consulting authoritative authorization state for sensitive operations.
- Using server-authoritative authentication modes where immediate state enforcement is required.

The appropriate approach depends on the security requirements of the application.

See [Authorization](/docs/plugin-domains/authorization) and [Policies & Access Control](/docs/plugin-domains/policies).

## Token Revocation

Revocation behavior differs fundamentally between opaque credentials and locally validated JWTs.

### Opaque Credential Revocation

An opaque credential depends on authoritative validation.

If the corresponding server-managed authentication state is revoked, subsequent validation can reject the credential.

The precise scope depends on whether the security operation targets a session, chain, or account-level root.

### JWT Revocation

A signed JWT cannot be made cryptographically invalid merely by deleting a server-side session record.

Its signature and expiration remain unchanged.

To reject an otherwise valid JWT before expiration, the resource must enforce an additional condition, such as an authoritative revocation check or another supported invalidation mechanism.

Without that additional enforcement, a locally validated JWT may remain acceptable until expiration.

### Refresh Revocation

Revoking a refresh credential can prevent further successful refresh operations.

However, that does not necessarily invalidate access tokens already issued before the revocation.

**Access revocation and refresh revocation are separate security concerns.**

## Session Security and Token Security

Token security and session security are closely related, but they operate at different levels.

| Concern | Token Security | Session Security |
|---|---|---|
| Credential format | Opaque or JWT | Root, chain, and session state |
| Cryptographic validation | JWT signature and claims | Not a substitute for token signature validation |
| Expiration | Token validity period | Session and chain lifecycle limits |
| Rotation | Replacement of authentication material | Continuity and associated state transitions |
| Revocation | Whether a presented credential remains acceptable | Which authentication state has been invalidated |
| Device context | May be associated with token use | Managed through applicable chain and session context |

A token may be valid according to its own format but unacceptable under authoritative session security rules.

Conversely, server-side session state may remain active while a particular access token has expired.

Both levels must be understood to reason correctly about the selected authentication mode.

## Common Security Misconceptions

### A Valid Signature Means Current Authorization

A valid JWT signature proves that the token has not been modified and was signed by an accepted key.

It does not prove that the user's permissions or account status have remained unchanged since issuance.

### A Device Identifier Prevents Token Theft

A device identifier may support context consistency checks.

It does not independently prevent a stolen bearer credential from being replayed.

### Refresh Rotation Makes Token Theft Impossible

Rotation can reduce reuse opportunities and support detection of invalid reuse.

It does not prevent a stolen credential from being used before the legitimate client or server detects the compromise.

### Logout Invalidates Every JWT Immediately

Logout may revoke server-managed session or refresh state.

Previously issued JWT access tokens can remain valid until expiration unless protected resources enforce an additional invalidation mechanism.

### Short-Lived Tokens Eliminate the Need for Session Security

Short lifetimes reduce the exposure window of a compromised token.

They do not replace account-level security policies, credential recovery protections, or session revocation controls where those capabilities are required.

## Security Design Principles

UltimateAuth's token model is best understood through a few principles.

**Use credentials only for their intended purpose.** Access and refresh credentials must not be interchangeable.

**Validate according to the selected mode.** Opaque credentials require authoritative validation; JWTs require correct cryptographic and claims validation.

**Treat JWT claims as issuance-time information.** Current authorization state may differ from what an older token contains.

**Keep access credentials appropriately short-lived.** Token lifetime should reflect the application's acceptable exposure window.

**Enforce refresh continuation rules.** Rotation, expiration, consumption, and reuse behavior must be consistent with the configured strategy.

**Distinguish token expiration from session revocation.** They invalidate authentication through different mechanisms.

**Do not promise immediate JWT revocation without enforcement.** Server-side state changes affect locally validated tokens only when the resource checks the relevant state.

## Architectural Perspective

UltimateAuth does not impose a single token model on every application.

PureOpaque and Hybrid emphasize server-authoritative authentication state.

SemiHybrid and PureJwt support locally validated JWT access, with different approaches to authentication continuation.

These choices affect performance, storage requirements, revocation behavior, and authorization freshness.

The purpose of the token security model is to make those tradeoffs explicit.

**A token establishes access only while its applicable validation requirements remain satisfied. The strength and freshness of those requirements depend on the authentication mode.**

## Next Steps

- [Session Security Model](/docs/security/session-security-model) — Understand authoritative session validation and hierarchical revocation.
- [Authentication Modes](/docs/fundamentals/authentication-modes) — Compare credential behavior across modes.
- [Session Model & Lifecycle](/docs/sessions-devices/session-model) — Explore session creation, continuation, and invalidation.
- [Credentials](/docs/plugin-domains/credentials) — Understand credential management and recovery security.