---
title: Refresh Rotation & Replay Protection
order: 3
group: security
---

# Refresh Rotation & Replay Protection

Refresh credentials allow authentication to continue without requiring the user to repeat the original login process.

Because a refresh credential may remain useful longer than an access credential, its compromise can have significant security consequences.

**Refresh rotation** is one mechanism for reducing this risk. It replaces previously issued refresh authentication material as part of a successful continuation operation.

However, secure rotation involves more than generating a new token. It requires consistent state transitions, concurrency control, invalid credential handling, and a clear policy for suspected credential reuse.

This page examines those security properties and their limitations.

For token formats, access token validation, and general refresh behavior, see [Token Security](/docs/security/token-security).

## Rotation as a Security Protocol

In a rotating refresh strategy, a successfully consumed refresh credential is replaced with new authentication material.

Conceptually:

```text
Refresh A
    |
    | Successful continuation
    v
Refresh A consumed
Refresh B issued
    |
    | Successful continuation
    v
Refresh B consumed
Refresh C issued
```

The security objective is to prevent an older refresh credential from remaining indefinitely reusable after a successful continuation.

A rotating refresh mechanism must maintain enough authoritative state to distinguish between credentials that are currently eligible for use and credentials that have already been consumed or invalidated.

This is fundamentally different from validating a self-contained access token using only its signature and expiration.

### What Rotation Does and Does Not Guarantee

Rotation can:

- Limit the continued usefulness of previously consumed credentials.
- Provide a signal when an invalidated credential is presented again.
- Support targeted security responses to suspicious continuation attempts.
- Reduce the opportunities for prolonged reuse of the same refresh credential.

Rotation does not inherently:

- Prevent the initial theft of a refresh credential.
- Prove that a reuse attempt was made by an attacker.
- Guarantee that the legitimate client wins a race against an attacker.
- Invalidate every previously issued access token.
- Guarantee immediate containment without an enforced revocation mechanism.

The strength of the protection depends on how the refresh strategy implements and validates its state transitions.

## Authentication Mode Considerations

Refresh behavior is not identical across UltimateAuth's authentication modes.

| Mode | Refresh and rotation considerations |
|---|---|
| **PureOpaque** | Uses opaque session continuation rather than a separate access/refresh credential pair |
| **Hybrid** | Uses opaque access and refresh credentials with server-authoritative session state |
| **SemiHybrid** | Uses JWT access tokens and server-managed refresh lifecycle state |
| **PureJwt** | Uses locally validated JWT access tokens and may use an optional refresh store |

The presence of a refresh operation does not necessarily mean that a separate refresh credential is replaced on every invocation.

Depending on the configured mode and strategy, a continuation operation may rotate credentials, update existing server-managed state, or follow another supported lifecycle behavior.

**Rotation guarantees must be evaluated against the selected refresh strategy, not assumed from the existence of a refresh endpoint.**

See [Authentication Modes](/docs/fundamentals/auth-modes).

## Refresh Credential State

A rotating refresh mechanism needs to distinguish credentials that remain eligible for continuation from credentials that are no longer acceptable.

Conceptually, relevant conditions include:

| Condition | Meaning |
|---|---|
| **Active** | The credential is eligible for a refresh attempt, subject to all applicable checks |
| **Consumed or replaced** | The credential has already participated in a successful rotation |
| **Revoked** | The credential or associated refresh authority has been explicitly invalidated |
| **Expired** | The credential has exceeded its permitted lifetime |
| **Invalid or unknown** | The presented value cannot be resolved to an acceptable refresh credential |

These are conceptual security conditions, not necessarily distinct persisted enum values in every implementation.

A consumed credential and an administratively revoked credential may both be unusable, but the reasons for rejection are different.

That distinction matters when evaluating whether a failed refresh represents ordinary expiration, an invalid request, or potential credential reuse.

## The Rotation Transaction

A successful rotating refresh operation involves a sequence of security-sensitive steps.

Conceptually:

1. Resolve the presented refresh credential.
2. Validate its eligibility for continuation.
3. Validate the applicable session, account, and client context.
4. Establish that the credential has not already been consumed.
5. Apply the configured rotation transition.
6. Issue the replacement authentication material.
7. Persist the resulting state consistently.
8. Return the renewed credentials to the client.

The exact implementation and ordering depend on the selected refresh strategy.

The important security requirement is that the transition must not leave multiple conflicting refresh credentials valid because two requests observed the same earlier state.

### Atomicity

Consider a refresh credential `A`.

Two requests arrive almost simultaneously:

```text
Request 1: Refresh(A)
Request 2: Refresh(A)
```

An unsafe implementation might allow both requests to read `A` as active before either marks it consumed.

Both requests could then issue different replacement credentials:

```text
               Refresh A
                  |
           +------+------+
           |             |
        Request 1     Request 2
           |             |
           v             v
       Refresh B     Refresh C
```

If both replacements remain valid, the system has unintentionally created competing continuation paths.

A single-use rotation strategy must prevent this outcome through an appropriate atomic state transition or concurrency mechanism.

Possible implementation techniques include transactional updates, compare-and-swap operations, or other storage-specific concurrency controls.

The precise mechanism is an implementation concern, but the security property is essential:

**Only the transition permitted by the configured rotation semantics may succeed.**

## Concurrent Refresh Requests

Concurrent refresh requests are not necessarily malicious.

They can occur when:

- Several API requests discover an expired access token at the same time.
- Multiple browser tabs attempt to refresh authentication.
- A client retries a request after a network interruption.
- Background and foreground operations initiate refresh simultaneously.

Without coordination, a legitimate client can submit the same refresh credential more than once.

### Client-Side Coordination

Applications should avoid issuing unnecessary concurrent refresh requests.

A common approach is to coordinate refresh through a single in-progress operation and allow other requests to await its result.

Conceptually:

```text
API Request A ──┐
API Request B ──┼──> One refresh operation
API Request C ──┘             |
                              v
                      Renewed credentials
                              |
                  +-----------+-----------+
                  |           |           |
                  v           v           v
               Retry A     Retry B     Retry C
```

Client coordination improves reliability and reduces accidental reuse attempts.

However, it is not a replacement for server-side concurrency enforcement.

Different devices, processes, or compromised clients cannot be assumed to share the same coordination mechanism.

### Server-Side Enforcement

The server remains responsible for enforcing the configured refresh credential lifecycle.

A client-side lock cannot guarantee that an attacker or another client instance will not present the same credential.

For single-use rotation, the server must ensure that a consumed credential cannot successfully establish another independent continuation path.

## Refresh Credential Reuse

Refresh credential reuse occurs when a credential is presented after it is no longer eligible for the operation.

For a single-use rotating strategy, presenting a previously consumed credential is a particularly important case.

```text
Initial state:
    Refresh A is active

First request:
    Refresh(A) succeeds
    A is consumed
    B is issued

Later request:
    Refresh(A) is attempted again
    A is no longer eligible
```

The later request must not be accepted as a new successful use of `A`.

### Reuse Is a Security Signal

A consumed refresh credential may be presented again because:

- An attacker copied the credential before rotation.
- A legitimate client retried after losing the refresh response.
- Two client operations raced to refresh simultaneously.
- Client credential storage was not updated consistently.

The server may not have enough information to distinguish these situations conclusively.

Therefore, **reuse detection identifies a suspicious credential lifecycle event; it does not automatically prove credential theft.**

### Detection Versus Response

Detecting invalid reuse and responding to it are separate responsibilities.

A refresh strategy may reject the request without taking broader action.

A stricter security policy may invalidate additional refresh state or revoke an associated session or chain.

The appropriate response depends on the implementation, authentication mode, and supported security policy.

A documentation claim that reuse always causes chain-wide revocation would be correct only if that behavior is actually enforced by the selected strategy.

## Replay and Token Theft Scenarios

Rotation is particularly useful when reasoning about stolen refresh credentials.

### Scenario 1: The Legitimate Client Refreshes First

An attacker copies refresh credential `A`, but the legitimate client uses it before the attacker.

```text
Legitimate client:
    A -> B

Attacker:
    Attempts A
    A is already consumed
```

If the rotation state is enforced correctly, the attacker's attempt to use `A` cannot produce another successful continuation.

The server may also be able to identify the attempt as reuse.

### Scenario 2: The Attacker Refreshes First

An attacker copies active refresh credential `A` and successfully uses it before the legitimate client.

```text
Attacker:
    A -> B

Legitimate client:
    Attempts A
    A is already consumed
```

In this case, the attacker may already possess the replacement credential `B`.

Rejecting the legitimate client's later use of `A` is necessary, but it does not by itself remove the attacker's newly obtained credential.

A broader revocation response may be required to contain the compromise.

This illustrates why rotation and reuse detection are not identical to automatic compromise recovery.

### Scenario 3: A Refresh Response Is Lost

The legitimate client sends `A`.

The server successfully consumes `A` and issues `B`, but the response never reaches the client.

The client still possesses `A`.

A retry may now appear to be reuse.

This creates an important reliability and security tradeoff.

Strict single-use semantics can reject the retry, while a more tolerant continuation strategy requires carefully defined behavior that does not accidentally create an exploitable replay window.

The chosen strategy must handle this situation consistently.

## Session and Chain Association

In session-managed authentication modes, refresh operations can be associated with session and chain state.

This allows the authentication infrastructure to evaluate continuation within the relevant security context.

For example, a refresh operation may need to establish that:

- The refresh credential belongs to the expected authentication state.
- The associated session remains eligible for continuation.
- The relevant chain has not been revoked.
- The account-level security state permits continuation.
- Any configured device context requirements are satisfied.

The exact checks depend on the mode and implementation.

### Chain-Level Containment

When a refresh compromise is associated with a session chain, chain-level revocation can provide a broader response than invalidating a single refresh credential.

This may be appropriate when multiple related sessions or continuation credentials could be affected.

However, a chain is a logical session continuity boundary, not a cryptographically verified physical device.

Chain-level revocation controls the associated authentication state; it does not permanently prevent the physical device from authenticating again.

See [Session Security Model](/docs/security/session-security).

## Invalid Refresh Conditions

Not every failed refresh should be classified as token reuse.

Different failure conditions have different security meanings.

| Condition | Expected security interpretation |
|---|---|
| Unknown credential | No acceptable refresh authority was established |
| Expired credential | The permitted continuation lifetime has ended |
| Explicitly revoked credential | The refresh authority has been invalidated |
| Previously consumed credential | Potential reuse under a single-use rotation strategy |
| Session or chain mismatch | Presented credential is inconsistent with expected authentication context |
| Account security invalidation | Continuation is no longer permitted under current account security state |
| Concurrent use | Multiple attempts may compete for the same continuation authority |

A secure implementation must reject invalid continuation attempts according to the selected strategy.

The application should not assume that every rejection requires the same logging, revocation, or user-facing response.

## Rotation and Access Token Validity

Refresh rotation affects authentication continuation.

It does not necessarily affect access tokens that were issued earlier.

For example:

```text
JWT Access A issued
Refresh A issued
        |
        v
Refresh A rotated to Refresh B
JWT Access B issued
```

If `JWT Access A` has not expired, it may remain valid under local JWT validation even after refresh rotation.

Similarly, revoking `Refresh B` may prevent future refresh operations without immediately invalidating `JWT Access B`.

This distinction is especially important in SemiHybrid and PureJwt.

### Revocation Scope

There are separate questions to answer:

- Can the old refresh credential be used again?
- Can the replacement refresh credential still be used?
- Is the associated server-managed session valid?
- Are previously issued access tokens still accepted?

A refresh security operation may affect some of these conditions without affecting all of them.

The authentication mode determines where current server-side state is consulted.

## Rotation and Credential Storage

Refresh rotation depends on maintaining authoritative information about credential eligibility.

For opaque refresh credentials, an implementation may store a one-way verifier or hash rather than the original credential value.

This reduces the exposure of usable credentials if the persistence store is disclosed.

However, secure storage alone does not provide rotation.

The implementation must also maintain the necessary lifecycle state and enforce transitions correctly.

### Operational Requirements

A production refresh mechanism should account for:

- Secure credential generation.
- Appropriate credential expiration.
- Protected transport and client storage.
- Consistent persistence updates.
- Concurrency control.
- Safe handling of invalid and consumed credentials.
- Revocation behavior appropriate to the authentication mode.

The exact persistence and concurrency mechanisms are implementation-specific.

## Security Tradeoffs

Strict rotation improves control over credential reuse, but it introduces operational complexity.

| Benefit | Corresponding consideration |
|---|---|
| Consumed credentials cannot remain reusable | Requires authoritative lifecycle state |
| Reuse attempts can become observable | Legitimate retries may resemble attacks |
| Continuation can be invalidated independently | Revocation must be consistently enforced |
| Replacement credentials limit prolonged reuse | Concurrent refresh requests require coordination |
| Compromise responses can target session state | Broader revocation may interrupt legitimate sessions |

These tradeoffs are not reasons to avoid rotation.

They are reasons to treat it as a security protocol rather than a simple token replacement operation.

## Security Design Principles

**Rotation must have defined semantics.** A refresh operation should not be described as single-use unless the configured strategy actually enforces single-use behavior.

**State transitions must be consistent.** Concurrent requests must not create conflicting valid continuation paths under a single-use strategy.

**Reuse detection is not theft attribution.** A reused credential is suspicious, but the cause may be malicious or accidental.

**Detection and containment are separate.** Rejecting reuse, revoking refresh state, and revoking a session chain are different security actions.

**Client coordination is helpful but insufficient.** The server must enforce credential eligibility independently.

**Access and refresh validity remain distinct.** Refresh rotation does not automatically invalidate previously issued JWT access tokens.

**Mode-specific behavior matters.** The same security guarantee must not be attributed to every authentication mode without considering its actual refresh implementation.

## Architectural Perspective

Refresh rotation is a mechanism for controlling how authentication authority continues over time.

Its security value comes from enforcing credential lifecycle transitions, preventing invalid continuation, and supporting an appropriate response when a previously consumed credential is presented again.

UltimateAuth's authentication modes provide different continuation models. Rotation behavior must therefore be understood in the context of the selected mode, its server-managed state, and its configured refresh strategy.

**A secure rotating refresh mechanism does not merely issue a new credential. It ensures that the previous credential can no longer establish an unauthorized continuation path.**

## Next Steps

- [Token Security](/docs/security/token-security) — Access and refresh credential security fundamentals.
- [Session Security Model](/docs/security/session-security) — Hierarchical validation and revocation boundaries.
- [Session Model & Lifecycle](/docs/sessions-devices/session-lifecycle) — Session continuity, expiration, and revocation.
- [Authentication Modes](/docs/fundamentals/auth-modes) — Mode-specific authentication and refresh behavior.