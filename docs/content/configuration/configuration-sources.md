# ⚙️ Configuration Sources

UltimateAuth supports multiple configuration sources.

👉 But more importantly, it defines a **clear and predictable precedence model**

## 🧠 Two Ways to Configure

UltimateAuth can be configured using:

### 🔹 Code (Program.cs)

```csharp
builder.Services.AddUltimateAuthServer(o =>
{
    o.Login.MaxFailedAttempts = 5;
});
```

### 🔹 Configuration Files (appsettings.json)

```json
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

## ⚖️ Precedence Rules

This is the most important rule:

👉 **Configuration files override code**

Execution order:

```
Program.cs configuration
        ↓
appsettings.json binding
        ↓
Final options
```

👉 This means:

- Defaults can be defined in code  
- Environments can override them safely  

<br>

## 🌍 Environment-Based Configuration

ASP.NET Core supports environment-specific configuration:

- appsettings.Development.json  
- appsettings.Staging.json  
- appsettings.Production.json

Example:

```json
{
  "UltimateAuth": {
    "Server": {
      "Session": {
        "IdleTimeout": "7.00:00:00"
      }
    }
  }
}
```

👉 You can use different values per environment without changing code

<br>

## 🧩 Recommended Strategy

For real-world applications:

### ✔ Use Program.cs for:
- Defaults  
- Development setup  
- Local testing  

### ✔ Use appsettings for:
- Environment-specific overrides  
- Production tuning  
- Deployment configuration  

👉 This keeps your system flexible and maintainable

<br>

## 🛡 Safety & Validation

UltimateAuth validates configuration at startup:

- Invalid combinations are rejected  
- Missing required values fail fast  
- Unsafe configurations are blocked  


👉 You will not run with a broken configuration


## ⚠️ Common Pitfalls

### ❌ Assuming code overrides config

It does not.

👉 appsettings.json always wins

### ❌ Hardcoding production values

Avoid:

```csharp
o.Token.AccessTokenLifetime = TimeSpan.FromHours(1);
```

👉 Use environment config instead for maximum flexibility


### ❌ Mixing environments unintentionally

Ensure correct environment is set:

```
ASPNETCORE_ENVIRONMENT=Production
```

<br>

## 🧠 Mental Model

If you remember one thing:

👉 Code defines defaults  
👉 Configuration defines reality  

## 📌 Key Takeaways

- UltimateAuth supports code + configuration  
- appsettings.json overrides Program.cs  
- Environment-based configuration is first-class  
- Validation prevents unsafe setups  
- Designed for real-world deployment

## ➡️ Next Step

Continue to **Advanced Configuration**
