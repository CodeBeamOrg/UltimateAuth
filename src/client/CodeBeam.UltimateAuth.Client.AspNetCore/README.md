# UltimateAuth Client ASP.NET Core Compatibility

Provides a lightweight ASP.NET Core compatibility layer for UltimateAuth client applications.

---

## 🎯 Purpose

This package enables seamless integration between **UltimateAuth client-side authentication** and **ASP.NET Core infrastructure**.

It allows applications to:

- Use `[Authorize]` without runtime errors
- Integrate with ASP.NET Core middleware pipeline
- Avoid mandatory server-side authentication setup

---

## ⚠️ Important

This package does **NOT** perform authentication.

It only provides a **No-op compatibility layer** for ASP.NET Core.

```text
✔ Prevents ASP.NET Core auth errors
✔ Enables framework compatibility
❌ Does NOT validate users
❌ Does NOT populate HttpContext.User
❌ Does NOT enforce security