---
title: Session Model & Lifecycle
order: 1
group: sessions-devices
---

# Session Model & Lifecycle

Authentication is more than issuing a credential and checking its expiration.

In stateful authentication systems, successful authentication establishes security state that must be managed throughout its lifetime. Sessions may be refreshed, extended, revoked, or invalidated in response to changes in account security.

UltimateAuth models this state through a structured hierarchy:

**Root → Chain → Session**

This hierarchy separates account-level authentication security, device-associated session continuity, and individual authentication sessions.

The model provides the foundation for session management, security versioning, targeted revocation, and lifecycle enforcement.

Its runtime behavior depends on the configured authentication mode.

## Understanding the Session Hierarchy

UltimateAuth organizes server-managed authentication state into three levels.

| Level | Represents | Primary responsibility |
|---|---|---|
| **SessionRoot** | Account-level authentication security | Global session state and security versioning |
| **SessionChain** | Related session continuity | Device association, activity, and chain-level control |
| **Session** | Individual authentication state | Session validity, expiration, and revocation |

Conceptually:

```text
User Identity
    |
    └── SessionRoot
          |
          ├── SessionChain (Desktop Browser)
          |      |
          |      ├── Session
          |      └── Session
          |
          └── SessionChain (Mobile Application)
                 |
                 └── Session
```

This is a logical representation of the hierarchy. It does not imply that every authentication mode stores the same records or maintains multiple simultaneous sessions within each chain.

Each level addresses a different security concern.

## SessionRoot: Account-Level Security

`SessionRoot` represents the account-level security boundary for server-managed authentication sessions.

It associates the authentication session hierarchy with a user identity and maintains security information relevant across that identity's sessions.

The root is not the user account itself.

The Users domain may own the user's identifiers, profile, and lifecycle. The session root belongs to authentication security management.

### Root Responsibilities

A session root provides a common boundary for:

- Associating session chains with an identity.
- Maintaining account-level session security state.
- Coordinating global session invalidation.
- Supporting security version checks.

The distinction is important because changes to authentication security do not necessarily require changes to the user entity.

For example, invalidating existing sessions should not require recreating the user or changing the user's identifiers.

### Security Versioning

UltimateAuth's session model supports security versioning.

A root can maintain a security version representing the current account-level authentication security state.

Sessions can retain the corresponding version from their creation.

Conceptually:

```text
Root.SecurityVersion = 4
Session.SecurityVersionAtCreation = 4

Result: Version matches
```

After an account-level security invalidation:

```text
Root.SecurityVersion = 5
Session.SecurityVersionAtCreation = 4

Result: Version mismatch
```

When the applicable session validation checks detect a version mismatch, the older session is no longer valid.

This allows the authentication system to invalidate previously established session state through a shared security boundary.

The effect on already-issued JWT access tokens still depends on whether protected requests consult current server-side security state.

## SessionChain: Session Continuity

`SessionChain` represents a logical grouping of related authentication sessions.

A chain can be associated with a particular device or client environment, allowing session activity to be managed within a recognizable continuity boundary.

For example, a user may have separate chains for a desktop browser and a mobile application.

However, a chain is not a physical device identity or a guarantee that the device has been cryptographically verified.

### Chain Responsibilities

A session chain can provide a boundary for:

- Associating related sessions.
- Maintaining device or client context.
- Recording relevant activity.
- Tracking session rotation.
- Applying chain-level security operations.
- Revoking related sessions together.

This allows UltimateAuth to distinguish between the lifecycle of an individual session and the continuity of the environment associated with it.

### Device Association

A chain may include device-related information such as a device identifier, platform, operating system, or browser.

These attributes help describe the client environment.

They must not be treated as independent proof of device ownership.

See [Device Management](/docs/sessions-devices/device-management) for the device association model and its security boundaries.

## Session: Individual Authentication State

A `Session` represents an individual unit of server-managed authentication state.

It is associated with a chain and participates in the validity rules established by the authentication mode and session configuration.

A session may contain information relevant to:

- Its associated identity and chain.
- Creation and expiration.
- Revocation status.
- Security version at creation.
- Authentication lifecycle and validity.

A session is not necessarily equivalent to an access token.

In opaque authentication modes, a client credential may reference server-managed session state.

In JWT-based modes, an issued access token may be validated locally without consulting the session hierarchy on every protected request.

**The token is authentication material. The session is a security-state concept.**

Keeping these concepts separate is essential for understanding UltimateAuth's authentication modes.

## Session Creation

A session lifecycle begins when an authentication flow successfully establishes the required authentication state.

For modes using the server-managed session hierarchy, the process conceptually involves:

1. Resolving the authenticated user identity.
2. Resolving or establishing the corresponding session root.
3. Determining the appropriate session chain.
4. Creating or updating the required session state.
5. Issuing the authentication material required by the selected mode.

The authentication flow must satisfy its verification and security requirements before establishing a valid authenticated session.

### Root Resolution

An existing account-level session root can be reused when appropriate.

If the required root does not exist, the session infrastructure can establish it as part of session creation.

### Chain Resolution

The session infrastructure determines whether authentication should be associated with an existing chain or a new chain.

A previously observed device identifier does not automatically establish that an existing chain is valid or eligible for reuse.

The decision must respect the applicable chain lifecycle and security conditions.

### Session Establishment

Once the appropriate security context has been established, the authentication system creates or updates the session state required by the authentication mode.

The resulting client credential is then issued according to that mode.

## Active Session Usage

After authentication, the client presents the appropriate authentication material when accessing protected resources.

What happens next depends on the configured mode.

In session-authoritative modes, protected requests can be evaluated against current server-managed session state.

In locally validated JWT modes, protected requests may be authenticated using the token's cryptographic validity and claims without loading the current session record.

This distinction affects session activity tracking, expiration enforcement, and revocation behavior.

### Touch Operations

A touch operation records qualifying session activity.

Depending on the session strategy, it may update information such as:

- `LastSeenAt`
- `TouchCount`
- Relevant session activity or expiration state

Touch behavior is not necessarily performed for every HTTP request.

Applications should not assume that an authenticated request always results in a persistent session update.

### Sliding Expiration

Sliding expiration allows eligible authentication state to remain valid as qualifying activity occurs.

Unlike a fixed expiration, sliding expiration can extend a session's usable lifetime according to configured rules.

However, sliding expiration does not necessarily remove absolute lifetime restrictions.

A session may still become invalid when a maximum permitted lifetime is reached.

The exact behavior depends on the selected authentication mode and its session configuration.

## Refresh and Rotation

Refresh and rotation are related to session lifecycle but are not identical operations.

**Refresh** is an authentication operation used to continue or renew authentication state.

**Rotation** replaces authentication material or session-related credentials according to the configured security strategy.

The relationship between these operations varies by authentication mode.

### PureOpaque

PureOpaque uses server-authoritative opaque session authentication.

It does not require a separate access-token and refresh-token pair.

Session continuity and activity management are handled through the applicable opaque session lifecycle.

### Hybrid

Hybrid uses distinct opaque access and refresh credentials backed by server-authoritative session state.

Refresh operations can renew authentication material while preserving the appropriate session or chain relationship.

The exact rotation behavior follows the configured refresh strategy.

### SemiHybrid

SemiHybrid uses JWT access tokens while maintaining server-side session state for lifecycle operations such as refresh and logout.

Refresh may issue new JWT access material and update the relevant server-managed state.

Already-issued access tokens may remain valid until expiration because protected requests do not necessarily consult that state.

### PureJwt

PureJwt primarily relies on stateless JWT validation for protected requests.

A refresh mechanism may use an independent refresh store, but the server-managed Root → Chain → Session hierarchy must not be assumed to govern every protected request.

This is an important distinction between the framework's common authentication concepts and the persistence requirements of individual modes.

See [Authentication Modes](/docs/fundamentals/authentication-modes).

## Session Expiration

Session expiration defines when authentication state is no longer eligible for continued use.

Depending on the authentication mode and configuration, expiration may involve:

| Expiration mechanism | Meaning |
|---|---|
| **Fixed lifetime** | Authentication state expires after a defined duration |
| **Idle timeout** | Authentication state expires after insufficient qualifying activity |
| **Sliding expiration** | Eligible activity extends the usable lifetime |
| **Absolute expiration** | Authentication state cannot extend beyond a maximum lifetime |

These mechanisms are not necessarily all enabled or applicable in every authentication mode.

### Expired Versus Revoked

Expiration and revocation represent different reasons for invalidation.

**Expiration** occurs because a configured time-based condition has been reached.

**Revocation** occurs because authentication state has been explicitly invalidated through a security operation.

Both can make a session unusable, but they represent different lifecycle events and may be recorded or enforced differently.

## Session Revocation

UltimateAuth's hierarchical session model supports security operations at different levels.

The scope of revocation determines how broadly authentication state is affected.

### Session-Level Revocation

Session-level revocation targets an individual session.

It is appropriate when a specific authentication instance must be invalidated without necessarily affecting other sessions associated with the same user.

### Chain-Level Revocation

Chain-level revocation targets the session continuity represented by a chain.

It allows related sessions to be invalidated together.

This can be useful when a particular client environment is suspected of compromise or when a user wants to terminate sessions associated with one device context.

Revoking a chain does not mean that the corresponding physical device is permanently blocked from authenticating again.

### Root-Level Revocation

Root-level revocation affects the account-level authentication security boundary.

It provides a mechanism for invalidating authentication state associated with the user's session hierarchy.

Security version changes can support this operation by making previously created sessions fail subsequent version validation.

This is useful for broad security responses, including operations that require invalidating all applicable existing sessions.

### Revocation Scope

Conceptually:

```text
Session revocation
    └── One session

Chain revocation
    └── Related sessions in one chain

Root revocation
    └── Account-level session hierarchy
```

The effective invalidation of protected requests depends on the authentication mode.

In particular, server-side revocation does not automatically invalidate a previously issued JWT that is still cryptographically valid and is not checked against current revocation state.

## Session Security and Credential Changes

Session lifecycle is closely related to credential security.

A password change or recovery operation may require existing authentication sessions to be preserved or revoked.

UltimateAuth supports session handling policies for credential changes and resets, including:

| Policy | Behavior |
|---|---|
| `KeepAll` | Preserve applicable existing sessions |
| `KeepCurrent` | Preserve the current session while revoking other applicable sessions |
| `RevokeAll` | Revoke all applicable sessions |

These policies allow applications to choose the security response appropriate to the operation.

For example, an authenticated password change and an anonymous password recovery operation may require different session handling.

The Credentials domain initiates the relevant security behavior, while the session infrastructure applies the corresponding lifecycle operations.

See [Credentials](/docs/plugin-domains/credentials).

## Session Configuration

Session behavior is configured through UltimateAuth's session options, including `UAuthSessionOptions`.

The configuration model governs supported aspects of session lifecycle such as:

- Session lifetimes.
- Activity and expiration behavior.
- Refresh and rotation strategies.
- Session and chain limits.
- Device-related session constraints.
- Revocation and lifecycle enforcement.

Not every option applies to every authentication mode.

For example, a configuration setting that controls server-side session activity may not affect the validation of a stateless JWT access token.

Applications should configure session behavior with the selected authentication mode and its enforcement boundaries in mind.

Detailed configuration examples belong in the configuration reference and How-to Guides.

## Architectural Perspective

UltimateAuth separates authentication identity, session continuity, and individual authentication state.

The Root → Chain → Session hierarchy expresses those responsibilities at different security levels:

- **Root** provides account-level authentication security.
- **Chain** organizes related session continuity.
- **Session** represents individual server-managed authentication state.

This structure supports targeted session management without requiring all security operations to act on every session belonging to a user.

At the same time, UltimateAuth does not assume that every authentication mode requires identical server-side state.

The hierarchy is most directly relevant to session-managed authentication, while JWT-based modes preserve their own validation and lifecycle characteristics.

**Authentication establishes trust. Session management determines how that trust is maintained, renewed, and invalidated over time.**

## Next Steps

- [Device Management](/docs/sessions-devices/device-management) — Device context, chain association, and device-aware session management.
- [Session Security](/docs/sessions-devices/session-security) — Session expiration, rotation, revocation, and security controls.
- [Authentication Modes](/docs/fundamentals/authentication-modes) — Understand how session behavior differs between modes.
- [Request Lifecycle](/docs/fundamentals/request-lifecycle) — Learn how protected requests validate authentication state.