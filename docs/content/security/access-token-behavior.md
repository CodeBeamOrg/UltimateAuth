# 🎯 Access Token Behavior

Access tokens in UltimateAuth are intentionally **short-lived and mode-aware**.
They are not the primary source of truth for authentication.

## 🧠 Core Principle

In UltimateAuth:

👉 Access tokens are **transport tokens**
👉 Sessions are the **source of truth**

## 🔑 Token Types

UltimateAuth supports two access token types:

### 🔹 Opaque Tokens

- Random, non-readable values
- Stored and validated server-side
- Typically used with session-based auth

### 🔹 JWT Tokens

- Self-contained tokens
- Contain claims (user, tenant, session, etc.)
- Signed and verifiable without storage

## ⚙️ Mode-Dependent Behavior

Access token behavior depends on auth mode:

### 🟢 PureOpaque

- No JWT issued
- Session cookie is the primary mechanism
- Access token may not exist externally

👉 Validation = session validation

### 🟡 Hybrid

- Opaque + JWT together
- Session still authoritative
- JWT used for API access

👉 Validation = session + token

### 🟠 SemiHybrid

- JWT is primary access token
- Session still exists
- Refresh rotation enabled

👉 Balanced approach

### 🔵 PureJwt

- Only JWT + refresh tokens
- No session state required

👉 Stateless mode

## ⏱ Lifetime Strategy

Access tokens are:

- short-lived
- replaceable
- not trusted long-term

Typical lifetime:

```text
5–15 minutes
```

## 🔄 Refresh Interaction

Access tokens are never extended directly.

Instead:

```text
Access Token → expires → Refresh → new Access Token
```

👉 This ensures:

- forward-only security
- no silent extension
- replay window minimization

## 🧾 Claims Model

JWT tokens may include:

- sub (user id)
- tenant
- sid (session id)
- jti (token id)
- custom claims

👉 Claims are generated at issuance time
👉 Not dynamically updated afterward

## ⚠️ Important Implication

If something changes:

- user roles
- permissions
- security state

👉 existing JWTs are NOT updated

👉 This is why:

- tokens are short-lived
- refresh is required
- session validation may still apply

## 🛡 Security Boundaries

Access tokens are:

❌ not revocable individually (JWT)
❌ not long-term identity

✔ tied to session or refresh flow
✔ bounded by expiration

## 🔥 Why This Matters

UltimateAuth avoids a common mistake:

👉 treating JWT as the system of record

Instead:

👉 JWT is a **snapshot**, not truth

## ⚠️ Design Tradeoff

Short-lived tokens mean:

- more refresh calls
- slightly more backend interaction

👉 But this enables:

- safer rotation
- better revocation
- reduced attack window

## 🧠 Mental Model

If you remember one thing:

👉 Access tokens are **temporary access grants**
👉 Not persistent identity

## ➡️ Next Step

Continue to **Policy Pipeline Deep Dive** to understand how access decisions are enforced.
