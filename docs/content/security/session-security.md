---
title: Session Security Model
order: 1
group: security
---

# Session Security Model

A valid authentication credential is not necessarily sufficient to establish that an authentication session should still be trusted.

A session may have been revoked, its associated account security state may have changed, or the client may no longer satisfy the conditions under which authentication was established.

UltimateAuth addresses these concerns through a **hierarchical session security model**, with explicit boundaries for session validity, device-associated continuity, and account-level invalidation.

In session-managed authentication modes, the security hierarchy is:

**Root → Chain → Session**

This page focuses on the security guarantees, validation requirements, and enforcement boundaries of that hierarchy.

For the underlying entities and their lifecycle, see [Session Model & Lifecycle](/docs/sessions-devices/session-lifecycle).

## Security Objectives

The session security model is designed to support several distinct requirements.

| Objective | Security purpose |
|---|---|
| **Session isolation** | Invalidate a particular authentication session without necessarily affecting others |
| **Chain isolation** | Control sessions associated with a particular device or client context |
| **Account-level invalidation** | Invalidate authentication state across the account's session hierarchy |
| **Security state propagation** | Detect changes to account-level authentication security |
| **Context consistency** | Ensure that related session, chain, root, and tenant information remains consistent |
| **Lifecycle enforcement** | Reject authentication state that has expired or been revoked |

These requirements are related, but they should not be collapsed into a single token expiration check.

A token can remain structurally valid even when the server-managed authentication state associated with it is no longer acceptable.

Whether that state is consulted during a protected request depends on the authentication mode.

## Security Boundaries

UltimateAuth's session hierarchy separates security responsibilities into three levels.

```text
Account-level security
        |
        v
   SessionRoot
        |
        |  Account-wide security state
        |  Security version
        |
        v
   SessionChain
        |
        |  Session continuity
        |  Device context
        |  Chain-level revocation
        |
        v
      Session
        |
        |  Individual validity
        |  Expiration
        |  Revocation
        |
        v
   Authentication state
```

Each level provides a distinct boundary for evaluating or invalidating authentication state.

### Root Security Boundary

The root represents account-level authentication security.

It provides a common authority for security state that affects sessions associated with the same account.

This enables account-wide invalidation without requiring the user identity itself to be deleted or recreated.

### Chain Security Boundary

A chain groups related authentication sessions and may be associated with a device or client environment.

It provides a boundary for revoking related sessions together.

A chain is not proof of physical device identity, and its existence does not automatically mean that the associated device is trusted.

### Session Security Boundary

A session represents individual server-managed authentication state.

It can be independently subject to expiration, revocation, and consistency checks.

A session is not equivalent to an access token: the token is presented by the client, while the session represents server-managed security state where the selected mode uses it.

## Layered Session Validation

In a session-authoritative authentication mode, accepting a presented credential requires more than recognizing its identifier.

The associated session hierarchy must satisfy the applicable validity and consistency requirements.

These requirements can be understood in several categories.

### Session Validity

The session must satisfy its own security conditions.

Relevant checks include:

- The session exists in the authoritative session store.
- The session has not been revoked.
- The session has not exceeded its applicable expiration limits.
- The session is associated with the expected chain.

A credential referencing a missing, expired, or revoked session must not establish a valid session-authoritative authentication state.

### Chain Validity

The associated chain must satisfy the applicable chain security conditions.

These may include:

- The chain exists.
- The chain has not been revoked.
- The chain belongs to the expected account and tenant context.
- The session-to-chain relationship is consistent.
- Any applicable chain-level expiration or inactivity conditions are satisfied.

Chain validation prevents an otherwise recognizable session from bypassing a security restriction applied to its broader session continuity boundary.

### Root Validity

The root must satisfy the account-level security requirements.

Relevant conditions include:

- The expected root exists.
- The root is associated with the expected identity and tenant context.
- The chain belongs to the expected root.
- The root has not been invalidated.
- Applicable security version requirements are satisfied.

This prevents a valid-looking session from being accepted when its account-level security authority has changed.

### Relationship Consistency

The hierarchy must also be internally consistent.

A session cannot be considered valid merely because a session record, a chain record, and a root record each exist independently.

Their ownership and association must match the expected authentication context.

This is especially important in multi-tenant applications, where a mismatched tenant or account association must not be accepted as a valid authentication relationship.

**The security decision concerns the entire applicable hierarchy, not an isolated record.**

## Security Versioning

Security versioning allows UltimateAuth to represent changes in account-level authentication security.

A root maintains a security version, and a session can retain the version associated with its creation.

During validation, the system can compare these values.

### Matching Security State

```text
Root.SecurityVersion = 7
Session.SecurityVersionAtCreation = 7

Security version check: PASS
```

The session was created under the current security version.

This does not mean that the session is automatically valid; expiration, revocation, and other applicable checks must still succeed.

### Changed Security State

```text
Root.SecurityVersion = 8
Session.SecurityVersionAtCreation = 7

Security version check: FAIL
```

The session was created under an earlier account-level security state.

When security version validation is enforced, the mismatch invalidates that session.

### Why Versioning Matters

Without a shared security version, an account-wide security change may require every individual session to be located and updated before the change can be enforced.

A security version provides a common condition against which existing session state can be evaluated.

This can support responses to events such as:

- Credential changes or recovery.
- Administrative security actions.
- Account compromise.
- Account-level session invalidation.

However, the triggering operation and configured session revocation policy determine whether a security version change should occur.

Not every credential change must automatically invalidate every session.

For example, UltimateAuth supports credential-related session handling policies such as `KeepAll`, `KeepCurrent`, and `RevokeAll`.

A security operation must preserve the intended policy semantics rather than indiscriminately invalidating the entire account hierarchy.

See [Credentials](/docs/plugin-domains/credentials).

## Device Context Validation

Device-associated session management can add security conditions to session validation.

A chain may be associated with a device identifier or other client context.

When device binding checks are configured and enforced, the system can compare the expected context with the context presented by a request.

For example:

```text
Chain.DeviceId  = device-A
Request.DeviceId = device-B

Device context mismatch
```

A configured mismatch policy may reject the operation.

However, this comparison must be interpreted correctly.

### Identification Is Not Authentication

A device identifier is an identifying value, not necessarily a secret.

If an attacker can copy or forge that value, matching the identifier does not prove that the request originated from the legitimate physical device.

Device context validation can enforce consistency with recorded metadata, but stronger device authentication requires an appropriate proof mechanism.

### Device Metadata Is Not a Security Guarantee

Operating system, browser, platform, and IP information may provide useful context.

They are not universally stable or unique.

Changes to this information can occur during legitimate usage, and matching values can occur across unrelated clients.

Applications should distinguish between:

- Informational device metadata.
- Configured device consistency requirements.
- Cryptographically established device binding.

The security guarantee must reflect the mechanism actually enforced.

See [Device Management](/docs/sessions-devices/device-management).

## Expiration and Inactivity

Expiration is a security control, not merely a cleanup mechanism.

UltimateAuth distinguishes between time-based validity and explicit security invalidation.

Depending on the configured mode and options, relevant conditions may include:

| Condition | Security meaning |
|---|---|
| Session lifetime | The authentication state has exceeded its permitted duration |
| Idle timeout | The required activity has not occurred within the allowed interval |
| Sliding expiration | Qualifying activity can extend validity within configured limits |
| Absolute lifetime | Authentication state cannot remain valid beyond a fixed boundary |
| Revocation | Authentication state has been explicitly invalidated |

A session may be expired without being revoked.

Similarly, a session may be revoked before reaching its natural expiration.

These conditions should remain distinguishable for correct enforcement and security diagnostics.

### Activity Tracking Is Not Universal

Session activity may be recorded through touch operations.

However, a locally validated JWT does not necessarily cause a server-side session touch on every protected request.

Therefore, inactivity enforcement depends on where authoritative activity state is maintained and when it is checked.

An application should not assume that all authentication modes provide identical idle-timeout guarantees.

## Hierarchical Revocation

Revocation allows authentication state to be invalidated before its normal expiration.

UltimateAuth's hierarchy supports different revocation scopes.

### Session-Level Revocation

Targets an individual session.

Other sessions may remain valid if their own security conditions continue to be satisfied.

This is appropriate for terminating a particular authentication instance.

### Chain-Level Revocation

Targets the session continuity associated with a chain.

It can invalidate related sessions without necessarily affecting other chains belonging to the same account.

This supports targeted containment when a particular client environment is suspected of compromise.

### Root-Level Revocation

Targets the account-level session security boundary.

It can invalidate the applicable session hierarchy, including chains and sessions associated with that root.

Security versioning can contribute to this mechanism by invalidating sessions created under earlier account-level security state.

### Revocation Scope and Enforcement

```text
Session revoke
    -> Targeted session

Chain revoke
    -> Sessions associated with the chain

Root revoke
    -> Account-level session hierarchy
```

These are logical revocation scopes.

The effect on protected requests depends on whether the authentication mode consults the revoked state.

**Revoking server-side state is not equivalent to cryptographically invalidating an already-issued JWT.**

## Authentication Mode Enforcement

The most important limitation of hierarchical session security is that its enforcement depends on the selected authentication mode.

| Mode | Protected request enforcement | Revocation considerations |
|---|---|---|
| **PureOpaque** | Uses authoritative server-side session validation | Revoked session state can be rejected on subsequent validation |
| **Hybrid** | Uses server-authoritative opaque credential and session validation | Session and chain revocation can be enforced through authoritative validation |
| **SemiHybrid** | JWT access tokens are validated locally for protected requests | Server-side revocation may not invalidate an already-issued JWT until expiration |
| **PureJwt** | JWT access tokens are primarily validated without a server-side session lookup | Immediate session-hierarchy revocation must not be assumed |

### Server-Authoritative Modes

PureOpaque and Hybrid can evaluate protected requests against current server-managed session state.

This allows applicable expiration, revocation, and security version conditions to affect subsequent authentication decisions.

### Locally Validated JWT Modes

SemiHybrid and PureJwt can accept a valid JWT without consulting current session hierarchy state on every protected request.

Consequently, changes to server-side session security may not immediately affect an already-issued access token.

The duration of this exposure depends on token lifetime and any additional validation or revocation mechanism configured by the application.

### Security Tradeoff

This is a deliberate architectural distinction.

Server-authoritative validation provides stronger immediate control over active authentication state at the cost of server-side validation work.

Locally validated JWTs reduce that dependency but cannot inherently provide the same immediate revocation behavior.

See [Authentication Modes](/docs/fundamentals/auth-modes).

## Threat Scenarios

The session security model helps address several common security scenarios.

### A Session Credential Is Stolen

An attacker obtains authentication material associated with one session.

If the compromise is detected, the affected session can be revoked.

In server-authoritative modes, subsequent attempts to use that session can be rejected through session validation.

If the attacker possesses an independently valid JWT, the result depends on JWT validation and revocation behavior.

### A Client Environment Is Compromised

An attacker may have access to authentication material associated with a particular browser or application installation.

Chain-level revocation provides a broader response than revoking only one session.

It can invalidate related session state while preserving unrelated chains where appropriate.

### An Account Is Compromised

An account-level security event may require invalidating authentication state across multiple devices.

Root-level invalidation provides a common security boundary for the applicable session hierarchy.

The operation must still account for the configured authentication mode and credential-related revocation policy.

### A Tenant Relationship Is Inconsistent

A session, chain, or root references an unexpected tenant or account relationship.

A session-authoritative validation process must not accept the credential solely because the individual records exist.

The relationships must satisfy the expected security context.

### A Device Identifier Is Spoofed

An attacker presents a device identifier matching a known client.

The identifier match alone does not establish device ownership.

The request must still satisfy the authentication and applicable session validation requirements.

These scenarios illustrate how different controls address different classes of risk.

No single session metadata field or policy provides complete protection against every form of credential compromise.

## Security Design Principles

The session security model follows several important principles.

**Validate relationships, not just identifiers.** Session, chain, root, account, and tenant associations must remain consistent.

**Separate authentication credentials from authoritative state.** A credential can reference security state without being equivalent to that state.

**Apply revocation at the appropriate scope.** Session, chain, and root invalidation serve different security purposes.

**Do not equate device recognition with device proof.** Client metadata provides context but not necessarily strong device authentication.

**Respect authentication mode boundaries.** A security control is only effective where its underlying state is actually checked.

**Keep lifecycle policy explicit.** Credential changes, recovery, expiration, and administrative actions may require different invalidation behavior.

## Architectural Perspective

UltimateAuth's hierarchical session model provides multiple security boundaries instead of relying on a single token validity check.

The root represents account-level security authority, the chain organizes related session continuity, and the session represents individual server-managed authentication state.

Together, they support targeted revocation, security versioning, relationship validation, and device-aware session control.

Their practical security guarantees depend on the authentication mode and the validation mechanisms enforced during protected operations.

**A secure session is not merely one that has not expired. It is one whose applicable security state, ownership relationships, and validation requirements still hold.**

## Next Steps

- [Session Model & Lifecycle](/docs/sessions-devices/session-lifecycle) — Understand the underlying hierarchy and lifecycle.
- [Device Management](/docs/sessions-devices/device-management) — Explore device context and chain association.
- [Authentication Modes](/docs/fundamentals/auth-modes) — Compare enforcement across authentication modes.
- [Credentials](/docs/plugin-domains/credentials) — Understand credential changes and session revocation policies.