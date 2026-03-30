\# 🚀 Getting Started



Welcome to \*\*UltimateAuth\*\* — the modern authentication framework for .NET.



UltimateAuth is designed to make authentication \*\*simple to use\*\*, while still being \*\*powerful, flexible, and secure\*\* enough for real-world applications.



\---



\## What is UltimateAuth?



UltimateAuth is a \*\*flow-driven authentication framework\*\* for modern .NET applications.



It unifies:



\- Session-based authentication

\- Token-based authentication (JWT)

\- PKCE flows for public clients

\- Multi-client environments (Blazor Server, WASM, MAUI, APIs)



into a single, consistent system.



Instead of choosing between cookies, sessions, or tokens,  

UltimateAuth allows you to use \*\*the right model for each scenario — automatically\*\*.



\---



\## Why UltimateAuth Exists



Authentication in modern .NET applications is fragmented:



\- ASP.NET Identity → user-focused but limited in flow flexibility  

\- JWT-based systems → stateless but hard to control and revoke  

\- OAuth solutions → powerful but complex and heavy  



UltimateAuth exists to solve this by providing:



\- A \*\*unified mental model\*\*

\- A \*\*consistent developer experience\*\*

\- A \*\*secure and extensible architecture\*\*



\---



\## What Problems It Solves



UltimateAuth addresses common real-world challenges:



\### 🔹 Multiple Client Types



Blazor Server, WASM, MAUI, and APIs all behave differently.  

UltimateAuth handles these differences automatically using \*\*Client Profiles\*\*.



\---



\### 🔹 Session vs Token Confusion



Should you use cookies, sessions, or JWT?



UltimateAuth removes this decision by supporting \*\*multiple auth modes\*\* and selecting the correct behavior at runtime.



\---



\### 🔹 Secure Session Management



\- Device-aware sessions  

\- Session revocation  

\- Refresh token rotation  

\- Replay protection  



All built-in — no custom implementation required.



\---



\### 🔹 Complex Auth Flows



Login, logout, refresh, PKCE, multi-device control etc.



UltimateAuth turns these into \*\*simple application-level operations\*\*.



\---



\## When to Use UltimateAuth



Use UltimateAuth if:



\- You are building a \*\*Blazor Server or WASM application\*\*

\- You need \*\*session + token hybrid authentication\*\*

\- You want \*\*full control over authentication flows\*\*

\- You are building a \*\*multi-client system (web + mobile + API)\*\*

\- You need \*\*strong security with extensibility\*\*



\---



\## When NOT to Use UltimateAuth



UltimateAuth may not be the right choice if:



\- You only need a \*\*very simple, stateless JWT-only API\*\*

\- You do not need session control, revocation, or device awareness

\- You prefer a fully external identity provider (Auth0, Azure AD, etc.)



\---



👉 Continue to \*\*Quick Start\*\* to build your first UltimateAuth application.

