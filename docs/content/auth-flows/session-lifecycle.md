---
title: Session Lifecycle
order: 5
group: auth-flows
---


# 🧬 Session Lifecycle
UltimateAuth is built around a structured session model.

👉 Authentication is not a token  
👉 It is a **hierarchical session system**

## 🧠 Core Model

UltimateAuth defines three core entities:
```
Root → Chain → Session
```

### 🔹 Root (Identity Authority)
- One per user  
- Represents global authentication state  
- Holds security version  

👉 Root defines **who the user is**

### 📱 Chain (Device Context)
- One per device  
- Represents a device-bound identity context  
- Tracks activity (LastSeenAt)  
- Manages session rotation and touch

👉 A chain is a **trusted device boundary**

### 🔑 Session (Authentication Instance)
- Created on login  
- Represents a single authentication event  
- Has expiration and revocation state  

👉 A session is a **proof of authentication**

<br>

## 🔗 Relationship
```
User
└── Root
└── Chain (Device)
└── Session (Instance)
```

👉 Each level adds more specificity:

- Root → identity  
- Chain → device  
- Session → login instance  

<br>

## 🔄 Lifecycle Overview

### 1️⃣ Creation (Login)
When a user logs in:

- Root is created (if not exists)  
- Chain is resolved or created  
- Session is issued  

### 2️⃣ Active Usage
During normal operation:

- Session is validated  
- Chain `LastSeenAt` is updated (touch)  
- Sliding expiration may apply  

👉 Activity updates the **chain**, not just the session

### 3️⃣ Refresh
Depending on mode:

#### Session-Based (PureOpaque)
- Session remains  
- Chain is touched  

#### Token-Based (Hybrid / JWT)
- Session continues  
- Tokens are rotated  
- Chain rotation count increases

👉 Chain tracks behavior:

- RotationCount  
- TouchCount  

### 4️⃣ Expiration
A session may expire due to:

- Lifetime expiration  
- Idle timeout  
- Absolute expiration

👉 Expired ≠ revoked

### 5️⃣ Revocation
Revocation can occur at multiple levels:

#### Session Revocation
- Single session invalidated

#### Chain Revocation
- All sessions on device invalidated  
- Device trust reset  

---

#### Root Revocation

- All chains and sessions invalidated  
- Security version increased  

👉 Revocation is irreversible

<br>

## 🔐 Security Model

### 🔒 Security Versioning
Each root has:
- `SecurityVersion`

Each session stores:
- `SecurityVersionAtCreation`

---

👉 If mismatch:

```text
Session becomes invalid
```

### 🔗 Device Binding
Each chain is tied to:

- DeviceId
- Platform
- OS
- Browser

👉 Prevents cross-device misuse

### 🔁 Rotation Tracking
Chains track:

- RotationCount
- TouchCount

👉 Enables:

- replay detection
- anomaly tracking

### ⚙️ Lifecycle Configuration
Session behavior is configurable:

⏱ Lifetime
- Default session duration

🔄 Sliding Expiration
- Extends session on activity

💤 Idle Timeout
- Invalidates inactive sessions

📱 Device Limits
- Max chains per user
- Max sessions per chain

👉 These are defined via UAuthSessionOptions

<br>

## 🧠 Mental Model
If you remember one thing:

👉 Authentication is a living structure
👉 Not a static token

## 📌 Key Takeaways
- Sessions are part of a hierarchy
- Device (chain) is a first-class concept
- Root controls global security
- Sessions are short-lived proofs
- Chains manage lifecycle and activity
- Revocation operates at multiple levels

## ➡️ Next Step

Continue to Token Behavior

