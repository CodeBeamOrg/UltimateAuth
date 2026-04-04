# ⚙️ Configuration & Extensibility

UltimateAuth is designed to be flexible.

But flexibility without structure leads to chaos.

👉 Configuration in UltimateAuth is structured, layered, and safe by default.

## 🧠 What You Configure

In UltimateAuth, you don’t just configure values.

👉 You configure behavior.

This includes:

- How sessions are created and managed  
- How tokens are issued and refreshed  
- How tenants are resolved  
- How clients interact with the system  
- Which features are enabled or restricted

<br>

## 🧩 Configuration Layers

UltimateAuth separates configuration into three layers:

### 🔹 Core

Defines authentication behavior:

- Session lifecycle  
- Token policies  
- PKCE flows  
- Multi-tenancy

### 🔹 Server

Defines runtime behavior:

- Allowed authentication modes  
- Endpoint exposure  
- Cookie and transport behavior  
- Hub deployment

### 🔹 Client

Defines client-side behavior:

- Client profile  
- PKCE configuration  
- Auto-refresh and re-authentication

👉 These layers are independent but work together.

## ⚙️ Configuration Sources

UltimateAuth supports two configuration styles:

### Code-based (Program.cs)

```csharp
builder.Services.AddUltimateAuthServer(o =>
{
    o.Login.MaxFailedAttempts = 5;
});
```

### Configuration-based (appsettings.json)
```csharp
{
  "UltimateAuth": {
    "Server": {
      "Login": {
        "MaxFailedAttempts": 5
      }
    }
  }
}
```

👉 appsettings.json overrides Program.cs

This allows:

- Environment-based configuration
- Centralized management
- Production-safe overrides

## 🛡 Safety by Design

UltimateAuth does not allow unsafe configurations silently.

- Invalid combinations fail at startup
- Unsupported modes are rejected
- Security invariants are enforced

👉 Flexibility is allowed
👉 Unsafe behavior is not

## 🎯 What’s Next?
- Understand configuration layers → Configuration Overview
- Learn Core behavior → Core Options
- Customize server → Server Options
- Control clients → Client Options
- Go deeper → Advanced Configuration
