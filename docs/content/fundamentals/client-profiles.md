# 🧩 Client Profiles
UltimateAuth adapts its authentication behavior based on the client.

👉 This is powered by **Client Profiles**.

<br>

## 🔑 What is a Client Profile?
A Client Profile defines how authentication behaves for a specific client type.

It affects:

- Authentication mode  
- Flow behavior  
- Token usage  
- Security constraints  

Unlike traditional systems:

👉 You don’t configure authentication globally  
👉 You let the system adapt per client

## 🧠 The Key Idea
> Authentication behavior is not static  
> It is determined **per client and per request**

<br>

## 🔄 Runtime Detection
By default, UltimateAuth automatically detects the client profile.

This is done using runtime signals such as:

- Loaded assemblies  
- Hosting environment  
- Registered services  

### Example Detection Logic
- MAUI assemblies → `Maui`
- WebAssembly runtime → `BlazorWasm`
- Server components → `BlazorServer`
- UAuthHub marker → `UAuthHub`
- Fallback → `WebServer`

👉 Detection happens inside the client at startup.

<br>

## 📡 Client → Server Propagation
The detected client profile is sent to the server on each request.

This can happen via:

- Request headers  
- Form fields (for flow-based operations)

```text
Client → (ClientProfile) → Server
```
On the server:

- The profile is read from the request
- If not provided → NotSpecified
- Server applies defaults or resolves behavior

<br>

## ⚙️ Automatic vs Manual Configuration
### Automatic (Default)
```csharp
builder.Services.AddUltimateAuthClientBlazor();
```
It means:
- AutoDetectClientProfile = true
- Profile is resolved automatically

### Manual Override
You can explicitly set the client profile:
```csharp
builder.Services.AddUltimateAuthClientBlazor(o =>
{
    o.ClientProfile = UAuthClientProfile.Maui;
    o.AutoDetectClientProfile = false; // optional
});
```

👉 This is useful when:
- Running in custom hosting environments
- Detection is ambiguous
- You want full control

<br>

## 🧩 Built-in Profiles

UltimateAuth includes predefined profiles:
| Profile      | Description                  |
| ------------ | ---------------------------- |
| BlazorServer | Server-rendered apps         |
| BlazorWasm   | Browser-based WASM apps      |
| Maui         | Native mobile apps           |
| WebServer    | MVC / Razor / generic server |
| Api          | API-only backend             |
| UAuthHub     | Central auth server          |

### 🛡 Safe Defaults
If no profile is specified (and auto detection is false):

- Client → NotSpecified → Server resolves safely
- Defaults are applied
- Unsafe combinations are prevented
- System remains consistent

### 🔐 Why This Matters
Client Profiles enable:

- Multi-client systems (Web + Mobile + API)
- Runtime adaptation
- Safe defaults without manual configuration

Without Client Profiles You would need:
- Separate auth setups per client
- Complex branching logic
- Manual security handling

## 🧠 Mental Model

If you remember one thing:

👉 Client defines behavior
👉 Server enforces rules

## 📌 Key Takeaways
- Client Profiles are automatically detected
- They are sent to the server on each request
- Behavior adapts per request
- You can override everything when needed
- Safe defaults are always applied

## ➡️ Next Step

Now that you understand runtime behavior:

👉 Continue to Runtime Architecture
