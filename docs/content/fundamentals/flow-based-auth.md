# 🔄 Flow-Based Authentication
UltimateAuth is not cookie-based or token-based.

👉 It is **flow-based**.

<br>

## 🔑 What Does “Flow-Based” Mean?
In traditional systems, authentication is treated as:

- A cookie  
- A JWT token  

Once issued, the system simply checks:

> “Is this token valid?”

<br>

UltimateAuth takes a different approach:

👉 Authentication is a **series of controlled flows**, not a static artifact.

<br>

## 🧭 Authentication as Flows
Every authentication operation is an explicit flow:

- **Login**
- **Logout**
- **Validate**
- **Refresh**
- **Re-authentication**

Each flow:

- Is initiated intentionally  
- Is processed on the server  
- Produces a controlled result  

<br>

## 🔁 Example: Login Flow
Instead of:

> “Generate a token and store it”

UltimateAuth does:
```
Login Request
→ Validate credentials
→ Resolve Root
→ Resolve or create Chain
→ Create Session
→ Issue authentication grant
```

👉 Login is not a single step — it is a **managed process**

<br>

## 🔄 Example: Refresh Flow
Traditional systems:

> Refresh = issue new token

UltimateAuth:
```
Refresh Request
→ Validate session
→ Check security constraints
→ Apply policies (if any)
→ Optionally rotate tokens
→ Update session state (if needed)
```

👉 The server decides what actually happens

<br>

## 🔍 Example: Validate Flow
On each request:
```
Incoming Request
→ Extract session/token
→ Validate session
→ Check chain (device context)
→ Verify root security version
→ Build auth state
```

👉 Validation is not just “token valid?”

<br>

## ⚠️ Why Token-Based Thinking Falls Short
Token-based systems assume:

- The token contains truth  
- The server trusts the token  

This leads to:

- Weak revocation  
- No device awareness  
- Limited control  

<br>

## ✅ UltimateAuth Approach
UltimateAuth treats tokens as:

👉 **transport artifacts**, not sources of truth

The real authority is:

- Root  
- Chain  
- Session  

<br>

## 🧠 Key Idea
> Tokens carry data  
> Flows enforce rules  

<br>

## 🔐 Server-Controlled by Design

All flows are:

- Executed on the server  
- Evaluated against policies  
- Subject to security constraints  

👉 The client does not control authentication state

<br>

## ⚙️ Flow Examples in Code

Using `IUAuthClient`:

```csharp
await UAuthClient.Flows.LoginAsync(request);
await UAuthClient.Flows.RefreshAsync();
await UAuthClient.Flows.LogoutAsync();
```
👉 Each method represents a server-driven flow

<br>

## 🧩 How This Changes Development
Instead of thinking:

❌ “I need to manage tokens”

You think:

✅ “I need to trigger flows”

<br>

## 📌 Benefits of Flow-Based Authentication
### ✔ Predictable Behavior
- Every action is explicit and controlled.

### ✔ Better Security
- No blind token trust
- Server-side validation
- Policy-driven decisions

### ✔ Extensibility
Flows can be extended with:

- MFA
- Risk-based checks
- Custom policies

### ✔ Consistent Across Clients
Same flows work for:
- Blazor Server
- WASM (PKCE)
- APIs

<br>

## 🧠 Mental Model
If you remember one thing:

👉 Authentication is not a token — it is a process

## ➡️ Next Step

Now that you understand flows:

👉 Continue to Auth Modes
