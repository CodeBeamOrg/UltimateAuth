# 🧠 Authentication Model

UltimateAuth is built around a simple but powerful idea:

> Authentication is not just a token.  
> It is a **structured, server-controlled session model**.

At the core of this model are three concepts:

- **Root**
- **Chain**
- **Session**

Together, they form what we call:

👉 **Authentication Lineage**

<br>

## 🔑 The Big Picture

Instead of treating authentication as a single token or cookie, UltimateAuth models it as a hierarchy:

```
Root (user authority)
├── Chain (device context)
│ ├── Session (login instance)
│ ├── Session
│
├── Chain
│ ├── Session (login instance)
```

Each level has a distinct responsibility.

<br>

## 🧩 Root — The Authority

**Root** represents the authentication authority of a user within a tenant.

- There is **only one active Root per user per tenant**
- It defines the **global security state**
- It controls all chains and sessions

### What Root Does

- Tracks security version (security epoch)
- Invalidates all sessions when needed
- Acts as the **source of truth**

### Example

If a user changes their password:

👉 Root is updated  
👉 All existing sessions can become invalid  

<br>

## 🔗 Chain — The Device Context

**Chain** represents a device or client context.

- Each device typically has its own chain
- Multiple logins from the same device belong to the same chain

👉 Think of Chain as:

> “Where is the user logged in from?”

### What Chain Does

- Groups sessions by device
- Enables **device-level control**
- Allows actions like:

  - Logout from one device  
  - Revoke a specific device  

<br>

## 🧾 Session — The Authentication Instance

**Session** is the actual authentication instance.

- Created when the user logs in
- Represents a single authenticated state
- Carries a snapshot of the Root security version

👉 This is what gets validated on each request.

### What Session Does

- Proves the user is authenticated
- Can be refreshed, revoked, or expired
- Is tied to a specific chain

<br>

## 🔄 How They Work Together

When a user logs in:

1. Root is resolved (or created)
2. A Chain is identified (device context)
3. A new Session is created

`Login → Root → Chain → Session`


On each request:

1. Session is validated  
2. Chain context is checked  
3. Root security version is verified  

👉 If any level is invalid → authentication fails

<br>

## 🛡 Why This Model Matters

Traditional systems rely on:

- Cookies  
- JWT tokens  
- Stateless validation  

These approaches have limitations:

- No real session control  
- Weak revocation  
- No device awareness  

---

UltimateAuth solves this by:

### ✔ Server-Controlled Authentication

- Sessions are always validated server-side  
- No blind trust in tokens  

### ✔ Instant Revocation

- Revoke a session → immediate effect  
- Revoke a chain → device logged out  
- Revoke root → disable user (global logout)

### ✔ Device Awareness

- Each device has its own chain  
- Sessions are bound to context  

### ✔ Strong Security Model

- Session carries Root security version
- Old sessions automatically become invalid

<br>

## 🧠 Mental Model

If you remember only one thing, remember this:

👉 **Root = authority**  
👉 **Chain = device**  
👉 **Session = login**

## 📌 Key Takeaways

- Authentication is not just a token  
- Sessions are first-class citizens  
- The server always remains in control  
- Device and security context are built-in

## ➡️ Next Step

Now that you understand the core model:

👉 Continue to **Flow-Based Authentication**
