---
title: Refresh
order: 3
group: auth-flows
---


# 🔄 Refresh Flow
The refresh flow in UltimateAuth is not a single fixed operation.

👉 It is a **mode-dependent continuation strategy**.

## 🧠 What is Refresh?
In traditional systems:

- Refresh = get a new access token  

In UltimateAuth:

👉 Refresh continues an existing authentication state

> Refresh is not re-authentication  
> → It is session continuation

## 🔀 Two Refresh Strategies
UltimateAuth supports two fundamentally different refresh behaviors:

### 🔹 Session Touch (Stateful)
Used in **PureOpaque mode**

- No tokens involved  
- No new session created  
- Existing session is extended  

```
Session → Validate → Touch → Continue
```

👉 This updates activity without changing identity

### 🔹 Token Rotation (Stateless / Hybrid)
Used in:

- Hybrid  
- SemiHybrid  
- PureJwt  

- Refresh token is validated  
- Old token is revoked  
- New tokens are issued  

```
RefreshToken → Validate → Revoke → Issue New Tokens
```

👉 This ensures forward security

<br>

## ⚖️ Mode-Based Behavior
Refresh behavior is determined by the authentication mode:

| Mode        | Behavior              |
|-------------|-----------------------|
| PureOpaque  | Session Touch         |
| Hybrid      | Rotation + Touch      |
| SemiHybrid  | Rotation              |
| PureJwt     | Rotation              |

👉 UltimateAuth automatically selects the correct strategy.

<br>

## 🔄 Step-by-Step Execution

### 1️⃣ Input Resolution
The system resolves:

- SessionId (if present)  
- RefreshToken (if present)  
- Device context  

### 2️⃣ Mode-Based Branching
The system determines the refresh strategy:

- Session-based → Touch  
- Token-based → Rotation  
- Hybrid → Both

### 3️⃣ Session Validation
If session is involved:

- Session is validated  
- Device binding is checked  
- Expiration is evaluated

### 4️⃣ Token Validation (if applicable)
If refresh token is used:

- Token is validated  
- Session and chain are verified  
- Device consistency is checked

### 5️⃣ Security Checks
The system enforces:

- Token reuse detection  
- Session validity  
- Chain integrity  

👉 If validation fails → reauthentication required

### 6️⃣ Execution
Depending on the strategy:

#### Session Touch
- Updates `LastSeenAt`  
- Applies sliding expiration  
- No new tokens issued

#### Token Rotation
- Revokes old refresh token  
- Issues new access token  
- Issues new refresh token

#### Hybrid Mode
- Validates session  
- Rotates tokens  
- Updates session activity

### 7️⃣ Response Generation
The response may include:

- SessionId  
- Access token  
- Refresh token  

👉 Output depends on mode and client

<br>

## 🔐 Security Model
The refresh flow includes strong protections:

### 🔁 Token Reuse Detection
If a refresh token is reused:

- Chain may be revoked  
- Session may be revoked  

👉 This prevents replay attacks

### 🔗 Session Binding
- Tokens are bound to session  
- Session is bound to device

👉 Prevents token misuse across devices

### 🧬 Chain Integrity
- Refresh operates within a chain  
- Cross-device usage is rejected  

<br>

## 🧠 Mental Model
If you remember one thing:

👉 Refresh = continuation  
👉 Not new authentication

## 📌 Key Takeaways

- Refresh behavior depends on auth mode  
- Stateful systems use session touch  
- Stateless systems use token rotation  
- Hybrid systems combine both  
- Security is enforced at every step

## ➡️ Next Step

Continue to **Logout Flow**
