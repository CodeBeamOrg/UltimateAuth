# 🔐 Auth Flows

Authentication in UltimateAuth is not a single operation.

👉 It is a **flow-driven system**.

<br>

## 🧠 What is an Auth Flow?
An auth flow represents a complete authentication operation, such as:

- Logging in  
- Refreshing a session  
- Logging out  

Each flow:

- Has a defined lifecycle  
- Runs through the orchestration pipeline  
- Produces a controlled authentication outcome  

👉 Instead of calling isolated APIs, you execute **flows**.

## 🔄 Why Flow-Based?
Traditional systems treat authentication as:

- A login endpoint  
- A token generator  
- A cookie setter  

👉 These approaches often lead to fragmented logic.

UltimateAuth solves this by:
- Structuring authentication as flows  
- Enforcing a consistent execution model  
- Centralizing security decisions  

<br>

## 🧩 What Happens During a Flow?
Every flow follows the same pattern:
```
Flow → Context → Orchestrator → Authority → Result
```

- The **flow** defines the intent  
- The **context** carries state  
- The **orchestrator** coordinates execution  
- The **authority** enforces rules  

👉 This ensures consistent and secure behavior across all operations.

<br>

## 🔐 Types of Flows
UltimateAuth provides built-in flows for common scenarios:

### 🔑 Login Flow
Establishes authentication by:

- Validating credentials  
- Creating session hierarchy (root, chain, session)  
- Issuing tokens if required  

👉 [Learn more](./login-flow.md)

### 🔄 Refresh Flow
Extends an existing session:

- Rotates refresh tokens  
- Maintains session continuity  
- Applies sliding expiration  

👉 [Learn more](./refresh-flow.md)

### 🚪 Logout Flow
Terminates authentication:

- Revokes session(s)  
- Invalidates tokens  
- Supports device-level or global logout  

👉 [Learn more](./logout-flow.md)

<br>

## 🧠 Supporting Concepts
These flows operate on top of deeper system models:

### 🧬 Session Lifecycle

- Root → Chain → Session hierarchy  
- Device-aware session structure  
- Lifecycle management and revocation  

👉 [Learn more](./session-lifecycle.md)

### 🎟 Token Behavior

- Access token vs refresh token  
- Opaque vs JWT  
- Mode-dependent behavior  

👉 [Learn more](./token-behavior.md)

### 📱 Device Management

- Device binding  
- Multi-device sessions  
- Security implications  

👉 [Learn more](./device-management.md)

<br>

## 🧠 Mental Model

If you remember one thing:

👉 **Authentication is not a single step**  
👉 **It is a controlled flow of state transitions**

## 📌 Key Takeaways

- Authentication is executed as flows  
- Each flow follows a consistent pipeline  
- Sessions and tokens are created as part of flows  
- Security is enforced centrally  

---

## ➡️ Next Step

Start with the most important flow:

👉 Continue to **Login Flow**
