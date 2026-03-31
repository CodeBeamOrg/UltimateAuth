# 🧩 Plugin Domains

Authentication alone is not enough.

Real-world systems also require:

- User management  
- Credential handling  
- Authorization rules  

👉 UltimateAuth provides these as **plugin domains**

## 🧠 What Is a Plugin Domain?

A plugin domain is a **modular business layer** built on top of UltimateAuth.

👉 Core handles authentication  
👉 Plugin domains handle identity and access logic  

<br>

## 🏗 Architecture

Each plugin domain is composed of multiple layers:

### 🔹 Bridge Package

Defines the server needed bridge interfaces:

👉 This package provides required minimal info for server package.

<br>

### 🔹 Contracts Package

Shared models between:

- Server  
- Client  

👉 Includes DTOs, requests, responses

### 🔹 Reference Implementation

Provides default behavior:

- Application services  
- Store interfaces  
- Default implementations  

👉 Acts as a production-ready baseline

### 🔹 Persistence Layer

Provides storage implementations:

- InMemory  
- Entity Framework Core

👉 Additional providers (Redis, etc.) can be added

<br>

## 🔄 Extensibility Model

Plugin domains are designed to be:

- Replaceable  
- Extendable  
- Composable  

👉 You can implement your own persistence  
👉 You can extend behavior

<br>

## ⚠️ Recommended Approach

In most cases:

👉 You should NOT replace a plugin domain entirely  

Instead:

- Use provided implementations  
- Extend via interfaces  
- Customize behavior where needed  

👉 This ensures compatibility with the framework

<br>

## 🧠 Mental Model

If you remember one thing:

👉 Core = authentication engine  
👉 Plugin domains = business logic  

## 🎯 Why This Matters

This architecture allows UltimateAuth to:

- Stay modular  
- Support multiple domains  
- Enable enterprise customization  
- Avoid monolithic identity systems  

👉 You don’t build everything from scratch  
👉 You assemble what you need

## ➡️ Next Step

- Manage users → Users
- Handle credentials → Credentials
- Control access → Authorization
