# UltimateAuth InMemory Bundle

Provides a complete in-memory setup for UltimateAuth.

## 🚀 Quick Start

```csharp
builder.Services
    .AddUltimateAuthServer()
    .AddUltimateAuthInMemory();
```

## 📦 Includes

- Reference domain implementations
- In-memory persistence for all modules

## 🎯 Use Cases

- Development
- Testing
- Prototyping

## ⚠️ Warning

Data is not persisted. Do not use in production.