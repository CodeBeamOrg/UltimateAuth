# 📱 Device Management

In UltimateAuth, devices are not an afterthought.

👉 They are a **first-class concept**

## 🧠 Why Device Matters

Most authentication systems ignore devices.

- A user logs in  
- A token is issued  
- Everything is treated the same

👉 This breaks down when you need:

- Multi-device control  
- Session visibility  
- Security enforcement  

👉 UltimateAuth solves this with **device-aware authentication**

## 🧩 Core Concept: Chain = Device

In UltimateAuth:

👉 A **SessionChain represents a device**

```
Device → Chain → Sessions
```

Each chain:

- Is bound to a device  
- Groups sessions  
- Tracks activity  

👉 A device is not inferred — it is explicitly modeled

## 🔗 What Defines a Device?

A chain includes:

- DeviceId  
- Platform (web, mobile, etc.)  
- Operating System  
- Browser  
- IP (optional binding)  

👉 This forms a **device fingerprint**

## 🔄 Device Lifecycle

### 1️⃣ First Login

- New device detected  
- New chain is created

### 2️⃣ Subsequent Logins

- Same device → reuse chain  
- New device → new chain  

👉 Device continuity is preserved

### 3️⃣ Activity (Touch)

- Chain `LastSeenAt` updated  
- `TouchCount` increases  

👉 Tracks real usage

### 4️⃣ Token Rotation

- Session changes  
- Chain remains  
- `RotationCount` increases  

👉 Device identity stays stable

### 5️⃣ Logout

- Session removed  
- Chain remains  

👉 Device still trusted

### 6️⃣ Revoke

- Chain invalidated  
- All sessions removed  

👉 Device trust is reset

<br>

## 🔐 Security Model

### 🔗 Device Binding

Sessions and tokens are tied to:

- Chain  
- Device context  

👉 Prevents cross-device reuse

### 🔁 Rotation Tracking

Chains track:

- RotationCount  
- TouchCount

👉 Enables anomaly detection

### 🚨 Revoke Cascade

If a device is compromised:

- Entire chain can be revoked  
- All sessions invalidated  

👉 Immediate containment

<br>

## ⚙️ Configuration

Device behavior is configurable via session options:

- Max chains per user  
- Max sessions per chain  
- Platform-based limits  
- Device mismatch behavior

👉 Fine-grained control for enterprise scenarios

<br>

## 🧠 Mental Model

If you remember one thing:

👉 Device = Chain  
👉 Not just metadata

## 📌 Key Takeaways

- Devices are explicitly modeled  
- Each device has its own chain  
- Sessions belong to chains  
- Security is enforced per device  
- Logout and revoke operate on device scope

## ➡️ Next Step

Continue to **Configuration & Extensibility**
