# UltimateAuth EntityFrameworkCore Bundle

Provides a complete production-ready setup for UltimateAuth using Entity Framework Core.

## 🚀 Quick Start

```csharp
builder.Services
    .AddUltimateAuthServer()
    .AddUltimateAuthEntityFrameworkCore(options =>
    {
        options.UseSqlServer("connection-string");
    });
```

## 📦 Includes

- Reference domain implementations

EF Core persistence for:

- Users
- Credentials
- Authorization
- Sessions
- Tokens
- Authentication

## ⚠️ Notes

- You must configure a database provider
- Migrations are not applied automatically