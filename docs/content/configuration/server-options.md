# 🧩 Server Options

UltimateAuth is configured primarily through **Server Options**.

👉 This is the main entry point for configuring authentication behavior.

## 🧠 What Are Server Options?

Server options define how UltimateAuth behaves **inside your application**.

They control:

- Authentication behavior  
- Security policies  
- Token issuance  
- Session lifecycle  
- Endpoint exposure  

<br>

## ⚙️ Basic Usage

You configure server options in `Program.cs`:

```csharp
builder.Services.AddUltimateAuthServer(o =>
{
    o.Login.MaxFailedAttempts = 5;
    o.Session.IdleTimeout = TimeSpan.FromDays(7);
});
```

---

You can also use `appsettings.json`:

```json
{
  "UltimateAuth": {
    "Server": {
      "Login": {
        "MaxFailedAttempts": 5
      },
      "Session": {
        "IdleTimeout": "07.00.00.00"
      }
    }
  }
}
```

👉 `appsettings.json` overrides `Program.cs`

## 🧩 Core Composition

Server options include Core behavior:

- Login  
- Session  
- Token  
- PKCE  
- Multi-tenancy  

👉 These are defined in Core

👉 But configured via Server

<br>

## ⚠️ Important: You Don’t Configure Modes Directly

UltimateAuth does NOT expect you to select a single auth mode.

Instead:

👉 Mode is resolved at runtime

<br>

## 🛡 Allowed Modes (Guardrail)

```csharp
o.AllowedModes = new[]
{
    UAuthMode.Hybrid,
    UAuthMode.PureOpaque
};
```

👉 This does NOT select a mode  
👉 It restricts which modes are allowed  

If a resolved mode is not allowed:

👉 Request fails early

<br>

## ⚡ Runtime Behavior (Effective Options)

Server options are not used directly.

They are transformed into:

👉 `EffectiveUAuthServerOptions`

This happens per request:

- Mode is resolved  
- Defaults are applied  
- Overrides are applied  

👉 What actually runs is **Effective Options**

<br>

## 🔄 Mode-Based Defaults

Each auth mode applies different defaults automatically:

- PureOpaque → session-heavy  
- Hybrid → session + token  
- PureJwt → token-only  


👉 You don’t need to manually configure everything

<br>

## 🎛 Endpoint Control

You can control which features are enabled:

```csharp
o.Endpoints.Authentication = true;
o.Endpoints.Session = true;
o.Endpoints.Authorization = true;
```


You can also disable specific actions:

```csharp
o.Endpoints.DisabledActions.Add("UAuthActions.Users.Create.Anonymous");
```

👉 Useful for API hardening

<br>

## 🍪 Cookie & Transport Behavior

Server options define how credentials are transported:

- Cookies  
- Headers  
- Tokens  

👉 Unsafe combinations are rejected at startup

<br>

## 🌐 Hub Configuration

If using UAuthHub:

```csharp
o.HubDeploymentMode = UAuthHubDeploymentMode.Integrated;
```

👉 Defines how auth server is deployed

<br>

## 🔁 Session Resolution

Controls how session IDs are extracted:

- Cookie  
- Header  
- Bearer  
- Query  

👉 Fully configurable

<br>

## 🧠 Mental Model

If you remember one thing:

👉 Server options define **what is allowed**  
👉 Runtime determines **what is used**

## 📌 Key Takeaways

- Server options are the main configuration entry  
- Core behavior is configured via server  
- Modes are not selected manually  
- Effective options are computed per request  
- Security is enforced by design  

---

## ➡️ Next Step

Continue to **Client Options**
