# UltimateAuth Reference Bundle

Provides a bundled setup of all UltimateAuth reference implementations.

## 📦 Included Modules

This package registers the default reference behavior for:

- Users
- Credentials
- Authorization

## 🚀 Quick Start

```csharp
builder.Services
    .AddUltimateAuthServer()
    .AddUltimateAuthReferences();
```

## 🧩 What This Does

This package wires together:
 
- Domain orchestration
- Validation pipelines
- Endpoint integrations
- Default application behavior

It allows you to get a fully working authentication system with minimal setup.

## ⚠️ Persistence Required

This package does NOT include persistence.

You must add one of the following:

- InMemory (for development)
`builder.Services.AddUltimateAuthInMemory();`

- Entity Framework Core (for production)
`builder.Services.AddUltimateAuthEntityFrameworkCore();`

## 🔧 Advanced Usage

If you need fine-grained control, you can install individual reference packages:

- CodeBeam.UltimateAuth.Users.Reference
- CodeBeam.UltimateAuth.Credentials.Reference
- CodeBeam.UltimateAuth.Authorization.Reference

## 🧠 Concept

Reference packages define the default domain behavior of UltimateAuth.

This bundle provides a ready-to-use composition of those behaviors.