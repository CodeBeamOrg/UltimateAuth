# UltimateAuth Client

Core client engine for UltimateAuth.

This package provides platform-agnostic authentication functionality including:

- Login / logout flows
- Token refresh handling
- PKCE support
- Session and state management
- Client-side domain services

---

## ⚠️ Important

This package does **NOT** include any platform-specific implementations such as:

- HTTP / JS transport
- Browser storage
- UI integration

To use this package in an application and for complete experience, you must install a platform adapter:

- CodeBeam.UltimateAuth.Client.Blazor

📦 Included automatically

You typically do NOT need to install this package directly.

It is included transitively by platform packages like: CodeBeam.UltimateAuth.Client.Blazor