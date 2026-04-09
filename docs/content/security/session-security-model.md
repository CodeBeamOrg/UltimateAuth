---
title: Session Security Model
order: 1
group: security
---


# 🔐 Session Security Model

UltimateAuth is built around a **hierarchical session model**.

This is the foundation of its security design.

## 🧠 Why a Session Model?

Many systems treat authentication as one of these:

- a cookie
- a bearer token
- a login flag

That works for simple apps.

It breaks down when you need:

- per-device isolation
- targeted revocation
- security state propagation
- reliable reauthentication boundaries

UltimateAuth solves this by modeling authentication as:

```text
Root → Chain → Session
```

<br>

## 🧩 The Three Layers

### 🔹 Root

A **Root** represents the user-level authentication authority.

It is:

- tenant-bound
- user-bound
- long-lived
- security-versioned

The root is the highest-level trust anchor for authentication state.

### 📱 Chain

A **Chain** represents a device-level authentication boundary.

It groups sessions that belong to the same device or client context.

A chain is where UltimateAuth models:

- device continuity
- touch activity
- rotation tracking
- device-level revoke

### 🔑 Session

A **Session** is a single authentication instance.

It is the most granular identity proof in the system.

A session contains:

- creation time
- expiration time
- revocation state
- security version snapshot
- chain relationship
- device snapshot
- claims snapshot


## 🔗 Relationship Model

```text
User
 └── Root
      └── Chain (device)
           └── Session (login instance)
```

👉 Root answers: **what is the current security authority for this user?**  
👉 Chain answers: **which device context is this?**  
👉 Session answers: **which authentication instance is being used?**

## 🛡 Security Versioning

One of the most important protections in UltimateAuth is **security versioning**.

A root maintains a security version.

Each session stores the security version that existed at creation time.

Validation compares them.

If they no longer match:

```text
Session → invalid
```

This is how UltimateAuth can invalidate existing sessions after events such as:

- password change
- credential reset
- account recovery
- administrative security action

## 🔍 Validation Model

Session validation is not a single check.

It is a layered verification process.

A session is considered valid only if all of these still hold:

- the session exists
- the session is active
- the chain exists
- the chain is active
- the chain belongs to the expected tenant
- the chain matches the session
- the root exists
- the root is not revoked
- the root matches the chain
- the root security version matches the session snapshot

## 📱 Device Awareness

Chains provide device-level isolation.

When a device identifier is available, validation can compare:

- device stored on chain
- device presented by request

If they do not match, UltimateAuth can reject the request depending on configuration.

👉 This makes device-bound security enforceable without turning every authentication flow into a custom implementation.

## ⏱ Expiration and Activity

UltimateAuth separates:

- session expiration
- chain activity
- root security state

This is important.

A session may expire because of time.  
A chain may become inactive because of idle timeout.  
A root may invalidate everything because security state changed.

These are different failure modes with different meanings.

## 🚪 Revocation Boundaries

Revocation is also hierarchical.

### Session revoke
Invalidates one authentication instance.

### Chain revoke
Invalidates all sessions for one device.

### Root revoke
Invalidates all chains and sessions for the user.

👉 This gives UltimateAuth targeted containment.

Instead of “log out everywhere or nowhere,”  
you can revoke exactly the right scope.

## 🔥 Why This Matters

This model gives you controls that flat token systems usually do not provide:

- revoke one device without affecting others
- invalidate sessions after security changes
- reason about device trust explicitly
- separate authentication lifetime from token lifetime

## ⚠️ Design Tradeoff

This model is more sophisticated than plain JWT-only authentication.

That is intentional.

UltimateAuth chooses:

- explicit state
- revocability
- traceable security decisions

over:

- minimal infrastructure
- purely stateless assumptions

## 🧠 Mental Model

If you remember one thing:

👉 Authentication in UltimateAuth is not “a token”  
👉 It is a **security hierarchy with revocation boundaries**

## ➡️ Next Step

Continue to **Refresh Token Rotation** to understand how continuation security works after login.
