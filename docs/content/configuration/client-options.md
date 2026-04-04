# 🧩 Client Options

Client Options define how UltimateAuth behaves on the **client side**.

👉 While Server controls the system,  
👉 Client controls how it is **used**

## 🧠 What Are Client Options?

Client options configure:

- Client profile (WASM, Server, MAUI, API)  
- Endpoint communication  
- PKCE behavior  
- Token refresh  
- Re-authentication behavior  

<br>

## ⚙️ Basic Usage

```csharp
builder.Services.AddUltimateAuthClientBlazor(o =>
{
    o.AutoDetectClientProfile = false;
});
```

<br>

## 🧩 Client Profile

UltimateAuth automatically detects client type by default:

- Blazor WASM  
- Blazor Server  
- MAUI  
- WebServer  
- API  

You can override manually:

```csharp
o.ClientProfile = UAuthClientProfile.BlazorWasm;
```

👉 Manual override is useful for testing or special scenarios

<br>

## 🌐 Endpoints

Defines where requests are sent:

```csharp
o.Endpoints.BasePath = "https://localhost:5001/auth";
```

👉 Required for WASM / remote clients

<br>

## 🔐 PKCE Configuration

Used for browser-based login flows:

```csharp
o.Pkce.ReturnUrl = "https://localhost:5002/home";
```

👉 Required for WASM scenarios

<br>

## 🔁 Auto Refresh

Controls token/session refresh behavior:

```csharp
o.AutoRefresh.Interval = TimeSpan.FromMinutes(1);
```

👉 Keeps authentication alive automatically

<br>

## 🔄 Re-authentication

Controls behavior when session expires:

```csharp
o.Reauth.Behavior = ReauthBehavior.RaiseEvent;
```

👉 Allows silent or interactive re-login

<br>

## 🧠 Mental Model

If you remember one thing:

👉 Server decides  
👉 Client adapts

## 📌 Key Takeaways

- Client options control runtime behavior  
- Profile detection is automatic  
- PKCE is required for public clients  
- Refresh and re-auth are configurable  
- Works together with Server options  

---

## ➡️ Next Step

Continue to **Configuration Sources**
