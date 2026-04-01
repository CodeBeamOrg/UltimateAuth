# 🎟 Token Behavior
In UltimateAuth, tokens are not the foundation of authentication.

👉 They are **derived artifacts of a session**

## 🧠 Rethinking Tokens
In traditional systems:

- Token = identity  
- Token = authentication  

In UltimateAuth:

👉 Session = identity  
👉 Token = transport mechanism  

> Tokens do not define identity  
> → Sessions do

## 🧩 Token Types
UltimateAuth supports two main token types:

### 🔹 Opaque Tokens
- Random, non-decodable values  
- Stored and validated on the server  
- Typically reference a session  

👉 Used in:

- PureOpaque  
- Hybrid

### 🔹 JWT (JSON Web Tokens)
- Self-contained tokens  
- Include claims and metadata  
- Signed and verifiable without server lookup

👉 Used in:

- SemiHybrid  
- PureJwt

<br>

## ⚖️ Mode-Based Behavior
Token behavior depends on the authentication mode:

| Mode        | Access Token  | Refresh Token  | Behavior                  |
|-------------|---------------|----------------|---------------------------|
| PureOpaque  | ❌            | ❌            | Session-only              |
| Hybrid      | ✔ (opaque/JWT)| ✔             | Session + token           |
| SemiHybrid  | ✔ (JWT)       | ✔             | JWT + session metadata    |
| PureJwt     | ✔ (JWT)       | ✔             | Fully stateless           |

👉 UltimateAuth selects behavior automatically

<br>

## 🔑 Access Tokens
Access tokens represent:

👉 A **temporary access grant**

### Characteristics
- Short-lived  
- Mode-dependent format  
- May contain session reference (`sid`)  
- May include claims  

### Important
Access token is NOT the source of truth.

👉 It reflects session state, not replaces it

## 🔄 Refresh Tokens
Refresh tokens represent:

👉 A **continuation mechanism**

### Characteristics
- Long-lived  
- Stored as hashed values  
- Bound to session and optionally chain  
- Rotated on use

### Lifecycle
Issued → Used → Replaced → Revoked

👉 Old tokens are invalidated on rotation

<br>

## 🔐 Security Model

### 🔁 Rotation
Each refresh:

- Invalidates previous token  
- Issues a new token  

👉 Prevents replay attacks

### 🚨 Reuse Detection
If a token is reused:

- Chain may be revoked  
- Session may be revoked  

👉 Strong forward security

### 🔗 Session Binding
Refresh tokens include:

- SessionId  
- ChainId (optional)  

👉 Prevents cross-context usage

### 🔒 Hashed Storage
Tokens are:

- Never stored as plaintext  
- Hashed using secure algorithms  

👉 Reduces breach impact

<br>

## 🔄 Token Issuance
Tokens are issued during:

- Login  
- Refresh  

### Access Token
- May be opaque or JWT  
- Includes identity and optional session reference

### Refresh Token
- Always opaque  
- Persisted in secure store  
- Used only for rotation  

<br>

## 🧠 Mental Model
If you remember one thing:

👉 Tokens are not identity  
👉 They are projections of a session

## 📌 Key Takeaways
- Session is the source of truth  
- Tokens are optional and mode-dependent  
- Opaque tokens require server validation  
- JWT tokens allow stateless access  
- Refresh tokens enable secure continuation  
- Token rotation ensures forward security  

## ➡️ Next Step

Continue to Device Management
