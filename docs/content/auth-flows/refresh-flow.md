---
title: Refresh
order: 3
group: auth-flows
---

# Refresh Flow

Refresh is how an existing authentication context continues without asking the user to authenticate again. In UltimateAuth, it is not synonymous with issuing a new access token: the operation depends on the active authentication mode and on whether the context is represented by a server-side session, refresh credentials, or both.

A successful refresh preserves the authenticated identity and its continuity. It does **not** prove the user's credentials again, create a new login, or necessarily extend every lifetime associated with the authentication.

## The model: continuity, not a second login

UltimateAuth distinguishes three related operations:

- **Authentication** establishes a new authenticated context after the required proof is accepted.
- **Refresh** attempts to continue an already established context, subject to its current validity and security policies.
- **Reauthentication** establishes trust again when continuation is unavailable or no longer permitted.

This distinction matters for both security and client behavior. A refresh request can fail because a session expired, a refresh credential is invalid or consumed, the device context does not match, or the underlying chain is no longer usable. The client should treat an unsuccessful refresh as a need to authenticate again, not as an invitation to retry the same credential indefinitely.

## Mode-dependent strategies

UltimateAuth implements refresh through two building blocks: **session touch** and **refresh-token rotation**. A mode selects how they are combined.

| Authentication mode | Required refresh input | Strategy | Result |
| --- | --- | --- | --- |
| `PureOpaque` | Session ID | Validate session; touch when policy requires | Continue the existing session; no new access or refresh token |
| `Hybrid` | Session ID **and** refresh token | Validate and conditionally touch session; rotate refresh token | Continue session and issue replacement tokens |
| `SemiHybrid` | Session ID **and** refresh token | Validate session; rotate refresh token | Issue replacement tokens without a session touch |
| `PureJwt` | Refresh token | Validate and rotate refresh token | Issue replacement tokens; no session touch in the flow |

**Current support status:** `PureOpaque` and `Hybrid` are the supported modes for the initial release. The `SemiHybrid` and `PureJwt` branches are present in the refresh implementation but should not be presented as completed, generally supported configurations in v0.1.0.

The mode is resolved from the effective authentication context on the server. Clients do not select a refresh algorithm independently.

## Session touch

A session touch is a controlled update to existing server-side authentication state. It does not issue a replacement credential or create a new session.

```text
Session ID
   → Validate session, chain and device context
   → Recheck active state and chain/session relationship
   → Touch if policy requires
   → Continue with the same session ID
```

In `PureOpaque`, the refresh flow requires a session ID. The session validator checks the session against the current tenant, device context and session options. The touch service then reloads the session and chain from the store and checks that they are still active and consistent, including the chain's active session, tenant, user and chain identifiers.

Touch behavior is governed by the effective session options:

- `TouchInterval` avoids writing activity state on every request. If the interval has not elapsed, the operation may succeed as **NoOp**.
- `TouchMode` is `IfNeeded` by default; `Force` requests a touch even when the normal interval has not elapsed.
- `SlidingExpiration` allows a touch to extend the session expiration using the configured `Lifetime`.
- An absolute chain expiration remains an upper bound; sliding expiration cannot extend the session beyond it.
- `IdleTimeout` participates in chain-state validation. An idle or otherwise inactive chain cannot be revived merely by touching it.

A successful **NoOp** is not a failed refresh: the existing state was valid, but no persistence update was necessary. Likewise, a touch is not an unconditional promise to extend the session's expiry; that depends on policy and the absolute limit.

## Refresh-token rotation

Rotation replaces a valid refresh credential while preserving its association with the existing authentication context.

```text
Refresh token
   → Validate token, tenant, device and expected session (if supplied)
   → Resolve user, session and chain
   → Prepare replacement refresh/access tokens
   → Atomically consume old refresh token and store replacement
   → Return new credentials
```

The rotation service first rejects modes or configurations that do not permit refresh-token issuance (`PureOpaque` or `IssueRefresh = false`). It then validates the presented credential and its context. The replacement refresh token and access token are prepared before the existing token is consumed. Persistence uses the refresh-token store's execution boundary and `TryConsumeAsync`, so competing requests cannot both successfully consume the same old token.

Rotation preserves the validated user, session and chain association; it is **not** a new authentication or a new session. Claims for the replacement access token are obtained through the configured claims provider.

### Reuse and concurrent requests

A consumed refresh token cannot be used again to obtain another replacement. UltimateAuth distinguishes two situations:

- **Likely concurrent request:** if a consumed token has a recorded replacement and the second request falls within `RefreshTokenConcurrentRequestWindow`, the second rotation is rejected without treating it as a confirmed replay.
- **Replay outside that window:** the rotation service revokes the associated refresh-token chain, or tokens associated with the session when a chain identifier is unavailable.

This distinction reduces destructive responses to ordinary client-side concurrency while retaining a response to suspicious reuse. The concurrency window is **not** a grace period in which the old token may be exchanged again: the second request still fails.

The rotation implementation shown here revokes refresh-token state. Do not interpret that as an unconditional guarantee that every related server-side session is also revoked; session invalidation is a separate concern.

## How each mode executes

### PureOpaque: session continuation

`PureOpaque` has no refresh-token rotation step. The endpoint resolves the existing session from the authentication flow context, validates it and performs a conditional touch. It returns `Touched` when state was updated or `NoOp` when the valid session required no update.

No access token or refresh token is issued. The existing session ID remains the credential representing the continued context.

### Hybrid: session and token continuity

`Hybrid` requires **both** a valid session ID and a refresh token. The flow:

1. Validates the session and device context.
2. Performs the session touch operation according to policy.
3. Validates and rotates the refresh token, requiring it to match the expected session.
4. Returns the existing session ID together with replacement access and refresh tokens.

The order is important: **session touch happens before token rotation** in the current implementation. These operations are not presented as one cross-store transaction. If rotation subsequently fails, the refresh response fails, although a preceding session touch may already have been persisted. This should not be described as an all-or-nothing operation.

### SemiHybrid and PureJwt: implementation branches

The current service includes branches for two additional modes:

- `SemiHybrid` requires a session ID and refresh token, validates the session and rotates the token, but does not touch the session.
- `PureJwt` uses the refresh token for rotation without a session touch step in `RefreshFlowService`.

These describe the existing code paths, **not a promise of v0.1.0 production support**. Configuration validation and supported-mode documentation remain authoritative for which modes applications can enable.

## Request resolution and response delivery

The refresh endpoint uses the server's `AuthFlowContext`. It obtains the session ID from `flow.Session`, the device from `flow.Device`, and the refresh token through `RefreshTokenResolver`.

For refresh-token input, the resolver checks:

1. The explicit `X-Refresh-Token` header, if present.
2. The configured refresh cookie, when cookie delivery is active.
3. The configured refresh header, when header delivery is active.

The explicit header takes precedence. Header values may be supplied with a `Bearer ` prefix, which the resolver strips.

On success, the endpoint chooses a **primary credential** according to the refresh response policy, rather than automatically writing every credential returned by the flow. The refresh token is written separately when permitted by the response policy and when a replacement exists. Actual cookie/header placement is determined by the effective response configuration and client profile.

The HTTP endpoint returns **`204 No Content`** on successful refresh and **`401 Unauthorized`** when continuation fails. Clients should obtain credentials from the configured response delivery mechanism rather than expecting a JSON token payload. When `Diagnostics.EnableRefreshDetails` is enabled, the endpoint also writes the refresh outcome through its diagnostic response writer.

> Response delivery customization is not fully independent in v0.1.0: mode templates and client-profile adaptation constrain the effective behavior. Do not assume every configured credential-delivery override is honored.

## Outcomes and client expectations

| Outcome | Meaning | Client interpretation |
| --- | --- | --- |
| `NoOp` | Existing session is valid; no touch was necessary | Continue normally |
| `Touched` | Existing session activity was updated | Continue normally |
| `Rotated` | Refresh token was consumed and replacement credentials issued | Replace stored credentials with the new values |
| `ReauthRequired` | The flow could not safely continue | Stop using the failed refresh credential and require authentication |

`RefreshFlowResult` also recognizes `Success` as a successful outcome, although the mode-specific branches described above return `NoOp`, `Touched`, `Rotated` or `ReauthRequired`.

Clients should serialize refresh attempts where practical. Parallel refresh requests using the same token do not produce multiple valid replacements, even when they fall within the concurrency window.

## Security boundaries

Refresh continues existing trust only while the relevant state remains valid:

- **Session and chain checks:** session-based paths validate active state, tenant, user, chain and session relationships.
- **Device context:** session and refresh-token validation receive the current device context; mismatches can prevent continuation.
- **Single-use refresh tokens:** successful rotation consumes the old credential and persists a replacement.
- **Replay response:** suspicious reuse can revoke refresh-token state associated with the chain or session.
- **Expiration:** idle, session, chain and refresh-token validity rules can end continuation. Refresh does not bypass those limits.
- **Issuance policy:** `IssueRefresh = false` prevents rotation before the old token is consumed.

These controls complement one another. A refresh token should be treated as a sensitive credential, not as a harmless request to generate another access token.

## Summary

The useful distinction is not simply *stateful versus stateless*. It is **what must remain valid for authentication to continue**:

- `PureOpaque` continues an existing server-side session, with conditional activity updates.
- `Hybrid` requires both session validity and successful refresh-token rotation.
- Other rotation branches exist, but are not part of the completed v0.1.0 supported-mode set.

Refresh does not authenticate a user again. It extends the usefulness of an already established authentication context only within the boundaries of the selected mode and configured security policies.

## Next step

Continue to [Logout Flow](./logout-flow).
