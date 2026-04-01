# 🔄 Refresh Token Rotation

Refresh tokens in UltimateAuth are not simple long-lived tokens.

They are part of a **rotation-based security system** designed to:

- prevent token replay
- detect token theft
- enforce forward-only session progression

## 🧠 Why Rotation?

In traditional systems:

- refresh tokens are long-lived
- they can be reused multiple times

👉 If stolen, an attacker can:

- silently keep refreshing access tokens
- stay undetected

UltimateAuth solves this with:

👉 **single-use refresh tokens (rotation)**

## 🔁 Rotation Model

Each refresh token is:

- used exactly once
- replaced with a new token
- linked to a chain

```text
Token A → Token B → Token C → ...
```

When a refresh happens:

1. Token A is validated  
2. Token A is **revoked**  
3. Token B is issued  
4. Token A is marked as replaced by B  

## 🔐 Token State

A refresh token can be:

- Active → valid and usable  
- Revoked → already used or manually revoked  
- Expired → lifetime exceeded  
- Replaced → already rotated  

👉 Only **active tokens** are valid

## 🚨 Reuse Detection

This is the most critical security feature.

If a refresh token is used **after it has already been rotated**, it means:

👉 The token was reused  
👉 Likely stolen  

### What happens?

When reuse is detected:

- the system identifies the session chain
- the entire chain can be revoked
- all related sessions become invalid

👉 This immediately cuts off both:

- attacker
- legitimate user (forcing reauthentication)

## 🔗 Chain Awareness

Refresh tokens belong to a **session chain**.

This enables:

- tracking rotation history
- detecting anomalies
- applying revocation at the correct scope

Without chains:

❌ You cannot safely detect reuse  
❌ You cannot know which tokens belong together  

## 🔄 Rotation Flow

```text
Client → Refresh(Token A)
        → Validate A
        → Revoke A
        → Issue B
        → Return new tokens
```

## ⚠️ Invalid Scenarios

### 1. Expired Token

```text
Token expired → reject
```

### 2. Revoked Token

```text
Token already used → reuse detected
```

### 3. Session Mismatch

```text
Token does not belong to expected session → reject
```

## 🧠 Security Guarantees

Rotation ensures:

- refresh tokens are forward-only  
- old tokens cannot be reused safely  
- stolen tokens are detectable  
- compromise triggers containment  

## 🔥 Why This Matters

Compared to traditional refresh tokens:

| Feature                | Traditional | UltimateAuth |
|----------------------|------------|-------------|
| Reusable tokens      | ✅         | ❌          |
| Reuse detection      | ❌         | ✅          |
| Chain awareness      | ❌         | ✅          |
| Automatic containment| ❌         | ✅          |

## ⚠️ Design Tradeoff

Rotation requires:

- token storage
- state tracking
- additional validation logic

👉 UltimateAuth chooses security over simplicity.

## 🧠 Mental Model

If you remember one thing:

👉 A refresh token is not a reusable key  
👉 It is a **one-time step in a chain**

## ➡️ Next Step

Continue to **Access Token Behavior** to understand how short-lived tokens interact with rotation.
