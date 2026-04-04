# 🔑 Login Flow
The login flow in UltimateAuth is not just credential validation.

👉 It is a **controlled session establishment process**.

<br>

## 🧠 What is Login?
In traditional systems:

- Validate credentials  
- Issue a token  

In UltimateAuth:

👉 Login creates a **session hierarchy**
```
Root → Chain → Session
```

> Login does not create a token  
> → It creates a session

Tokens are optional outputs derived from the session.

<br>

## 🔄 Step-by-Step Execution
The login flow follows a structured pipeline:

### 1️⃣ Identifier Resolution
The system resolves the user identity:

- Username / email / phone → `UserKey`

### 2️⃣ User & Security State
The system loads:

- User existence  
- Account state  
- Factor (credential) state  

Checks include:

- Is the account locked?  
- Is reauthentication required?

### 3️⃣ Credential Validation
Credentials are validated using providers:

- Password  
- (Extensible: OTP, external providers, etc.)

### 4️⃣ Authority Decision
The **LoginAuthority** evaluates the attempt.

Possible outcomes:

- ✅ Allow  
- ❌ Deny  
- ⚠️ Challenge (e.g. MFA)

👉 No session is created before this decision.

### 5️⃣ Device & Chain Resolution
The system checks if the device is known:

- Existing device → reuse chain  
- New device → create new chain  

👉 A **Chain represents a device**

### 6️⃣ Session Creation
A new session is issued:

- Linked to user  
- Linked to device (chain)  
- Bound to tenant  

Session hierarchy:
```
User → Root → Chain → Session
```

### 7️⃣ Token Issuance (Optional)
Depending on the mode and request:

- Access token may be issued  
- Refresh token may be issued  

👉 Tokens are derived from the session  
👉 Not the source of truth

### 8️⃣ Event Dispatch
The system emits:

- Login events  
- Audit information  

<br>

## 🧩 What Gets Created?
A successful login creates:

### 🔹 Root
- One per user  
- Represents global authentication state

### 🔹 Chain
- One per device  
- Manages device lifecycle

### 🔹 Session
- Individual authentication instance  
- Represents a single login

<br>

## 📱 Device Awareness
UltimateAuth is device-aware by design:

- Each device gets its own chain  
- Sessions are grouped by device  
- Logout can target device or all sessions  

<br>

## 🔐 Security Considerations
The login flow includes built-in protections:

- Account lockout
- Failed attempt tracking
- Device binding
- Security version validation

👉 Security decisions are centralized in the Authority

<br>

## 🧠 Mental Model
If you remember one thing:

👉 Login = session creation  
👉 Not token issuance

## 📌 Key Takeaways

- Login is a flow, not a function  
- Authority decides before any state change  
- Sessions are the source of truth  
- Tokens are optional representations  
- Device context is always considered

## ➡️ Next Step

Continue to **Refresh Flow**
