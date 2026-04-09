---
title: Logout
order: 4
group: auth-flows
---


# 🚪 Logout Flow
The logout flow in UltimateAuth is not a single action.

👉 It represents different **levels of authentication termination**.

## 🧠 What is Logout?
In traditional systems:

- Logout = remove cookie or token

In UltimateAuth:

👉 Logout affects **session, device, or identity scope**

> Logout is not just removing access  
> → It is controlling session lifecycle

## 🔀 Logout vs Revoke
UltimateAuth distinguishes between two concepts:

### 🔹 Logout (Soft Termination)

- Ends the current session  
- Keeps the device (chain) active  
- Allows re-login without resetting device context  

```
Session → Invalidated
Chain → Still Active
```

👉 User can log in again and continue on the same device chain

### 🔥 Revoke (Hard Invalidation)
- Invalidates session, chain, or root  
- Cannot be undone  
- Forces a completely new authentication path  

```
Chain → Revoked
Sessions → Revoked
Next login → New chain
```

👉 Revoke resets trust for that scope

<br>

## 🧩 Levels of Termination
UltimateAuth supports multiple logout scopes:

### 🔹 Session-Level Logout
- Terminates a single session  
- Other sessions on the same device may remain  

### 📱 Device-Level (Chain)
- Terminates all sessions on a device  
- Device chain is invalidated or reset

### 🌐 Global Logout (All Devices)
- Terminates all sessions across all devices  
- Keeps root (user identity) intact

### 🔥 Root Revoke
- Invalidates entire authentication state  
- All chains and sessions are revoked  

👉 This is the strongest possible action

<br>

## 🔄 Step-by-Step Execution

### 1️⃣ Flow Context Resolution
The system resolves:

- Current session  
- User identity  
- Tenant

### 2️⃣ Authority Decision
Logout operations are validated:

- Authorization checks  
- Access validation

👉 Logout is not blindly executed

### 3️⃣ Scope Determination
The system determines what to terminate:

- Session  
- Chain  
- Root

### 4️⃣ Execution
Depending on scope:

#### Session Logout
- Session is revoked  
- Other sessions unaffected

#### Chain Revoke / Logout
- All sessions in the chain are revoked  
- Device trust is reset

#### Global Logout
- All chains are revoked (optionally excluding current)

#### Root Revoke
- Entire identity state is invalidated

### 5️⃣ Event Dispatch
The system emits:

- Logout events  
- Audit logs  

<br>

## 📱 Device Awareness
Logout behavior is device-aware:

- Each device is a chain  
- Logout can target specific devices  
- Sessions are grouped by device  

👉 This enables fine-grained control

<br>

## 🔐 Security Model

### 🔒 Controlled Termination
All logout operations:

- Pass through orchestrator  
- Are validated by authority  

👉 Prevents unauthorized session manipulation

### 🔁 Irreversible Revocation
- Revoked chains cannot be restored  
- Revoked sessions remain invalid

👉 Ensures strong security guarantees

### 🔗 Identity Boundaries

- Session → temporary identity proof  
- Chain → device trust boundary  
- Root → global identity state  

👉 Logout operates within these boundaries

<br>

## 🧠 Mental Model
If you remember one thing:

👉 Logout = ending a session  
👉 Revoke = resetting trust

## 📌 Key Takeaways

- Logout and revoke are different operations  
- Logout is reversible (via re-login)  
- Revoke is permanent and forces new authentication  
- Device (chain) is a first-class concept  
- Security is enforced through authority and orchestrator

## ➡️ Next Step

Continue to **Session Lifecycle**
