---
title: Client Profiles
order: 5
group: fundamentals
---

# Client Profiles

UltimateAuth is designed to support different application environments through a unified authentication architecture.

A Blazor Server application, a browser-based WebAssembly application, a native mobile client, and a server-side application do not interact with authentication credentials in exactly the same way.

They may differ in how credentials are stored, how requests are sent, whether browser cookies are available, and how authentication results are delivered.

**Client Profiles allow UltimateAuth to account for these runtime differences without requiring separate authentication architectures for each client type.**

A Client Profile describes the environment in which a client operates. It helps the framework resolve suitable authentication behavior and defaults, while the server remains responsible for enforcing the effective security policy.

## What is a Client Profile?

A Client Profile identifies a supported client environment and its relevant runtime characteristics.

It can influence how UltimateAuth resolves:

- Default authentication mode.
- Credential delivery and transport.
- Client-specific authentication flow behavior.
- Browser and non-browser integration requirements.
- Applicable server configuration and compatibility constraints.

A profile does not authenticate the client or grant permissions.

For example, identifying a request as originating from a Blazor WebAssembly application does not establish the identity of its user.

Likewise, a client declaring itself to be a server-side application must not automatically receive privileges associated with a trusted server.

The distinction is fundamental:

**Client Profile describes the environment. Authentication Mode defines the authentication strategy. Server policy determines what is permitted.**

## Why Client Profiles exist

Consider an application that includes a browser frontend, a native mobile application, and a server-rendered administrative interface.

All three clients may authenticate against the same UAuthHub.

However, their runtime characteristics differ.

A browser application can use cookies managed by the browser. A native application may need to manage credentials through platform-specific secure storage. A server-rendered application may be able to maintain authentication through a server-controlled session without introducing a separate refresh-token lifecycle.

Without a client abstraction, these differences tend to spread throughout application code.

Developers may need separate configuration paths, credential-handling logic, and authentication integration code for every runtime.

Client Profiles provide a consistent way to describe those differences.

They allow UltimateAuth to select appropriate integration behavior while preserving the same underlying authentication and authorization architecture.

## Built-in Client Profiles

UltimateAuth defines profiles for its supported client environments.

| Profile | Environment | Typical characteristics |
|---|---|---|
| `BlazorServer` | Blazor Server applications | Server-side execution, browser connection, server-managed authentication context |
| `BlazorWasm` | Blazor WebAssembly applications | Browser execution, HTTP-based API communication, browser credential constraints |
| `Maui` | .NET MAUI applications | Native execution, platform-specific credential storage and HTTP communication |
| `WebServer` | ASP.NET Core MVC, Razor Pages, and other server-hosted web applications | Server-side execution with browser-oriented authentication |
| `Api` | API-oriented integrations | HTTP request authentication without assuming a browser UI |
| `UAuthHub` | UltimateAuth authentication host | Authentication-host-specific runtime integration |
| `NotSpecified` | No explicit profile resolved | Requires the applicable server-side fallback or resolution policy |

These profiles describe runtime environments rather than independent authentication systems.

For example, `BlazorWasm` does not inherently mean JWT authentication, and `BlazorServer` does not inherently prohibit access tokens.

A profile may provide a recommended default, but the authentication mode remains a separate decision.

### API profile versus resource API

An API-oriented client and an API acting as a protected resource are not necessarily the same thing.

A protected resource API may receive requests from browser applications, mobile applications, or other services.

The calling client's profile should not be confused with the hosting environment of the resource API.

Resource authentication must follow the applicable server-side configuration and token-validation requirements, regardless of what an untrusted caller claims its runtime to be.

## Automatic profile detection

UltimateAuth supports automatic Client Profile detection in compatible client integrations.

Detection is intended to reduce configuration requirements for common application environments.

Depending on the client implementation, the runtime can provide information such as:

- The current execution environment.
- Available framework capabilities.
- Hosting-specific registrations.
- Runtime and platform characteristics.

For example, a Blazor WebAssembly client can identify that it is executing in a browser environment, while a native MAUI client can identify its native application context.

This resolution occurs within the supported client integration.

It should not be confused with a server independently discovering the true identity or trust level of a remote application.

### Detection is a convenience, not a security boundary

Runtime detection helps choose appropriate defaults.

It is not an authentication proof.

A malicious HTTP client can imitate headers, request parameters, and other client-provided metadata.

Consequently, the server must not grant additional authentication privileges solely because a request claims a particular Client Profile.

The server remains responsible for validating the profile against its own configuration and security requirements.

## Client-to-server propagation

For supported operations, the client can communicate its resolved profile to the authentication server.

The transport may depend on the integration and operation, including request metadata such as headers or flow-specific form fields.

```text id="0mrrkv"
Client Runtime
      |
      v
Client Profile Detection
      |
      v
Profile Metadata
      |
      v
UAuthHub
      |
      v
Server-Side Profile Resolution
      |
      v
Effective Authentication Context
```

The important distinction is between a **declared profile** and an **effective profile**.

A declared profile is information supplied by the client.

An effective profile is the result accepted or resolved by the server under its configured rules.

The server must not treat arbitrary client-provided profile values as authority to weaken authentication requirements.

### Missing profile information

Some requests may not provide an explicit Client Profile.

In those cases, UltimateAuth can represent the unresolved client profile as `NotSpecified` and apply the relevant server-side resolution or fallback behavior.

A missing profile must not automatically imply a privileged or less restrictive environment.

If the server cannot establish a permitted configuration for the operation, it should reject the operation rather than inventing an unsafe authentication context.

## Automatic and explicit configuration

Client Profile resolution can be automatic or explicitly configured, depending on the supported client integration.

### Automatic configuration

For supported Blazor client registrations, automatic detection can reduce the need to specify the runtime manually.

```csharp
builder.Services.AddUltimateAuthClientBlazor();
```

The client integration can resolve the appropriate profile according to its runtime detection configuration.

This is generally the preferred approach when the application uses a standard, supported hosting environment.

### Explicit configuration

Applications can also configure the profile directly when their integration supports it.

```csharp
builder.Services.AddUltimateAuthClientBlazor(options =>
{
    options.ClientProfile = UAuthClientProfile.BlazorWasm;
    options.AutoDetectClientProfile = false;
});
```

Explicit configuration may be useful when:

- An application uses a custom hosting environment.
- Runtime detection is ambiguous.
- A shared integration requires a specific client profile.
- The application needs predictable profile selection across environments.

Explicitly configuring a client profile does not override the server's authentication policies.

It only determines what profile the client integration declares or uses locally.

The server must still resolve and enforce the effective authentication context.

## Client Profiles and Authentication Modes

Client Profiles and Authentication Modes are related but independent concepts.

A Client Profile describes the execution environment and its integration requirements.

An Authentication Mode defines how authentication credentials are issued, validated, continued, and revoked.

For example, a Blazor Server application commonly benefits from PureOpaque because it can use server-controlled sessions without managing separate access and refresh tokens.

A Blazor WebAssembly application communicating with a separate API may benefit from Hybrid because it can use access and refresh tokens while retaining server-authoritative session validation.

These are architectural recommendations, not inherent limitations of the runtimes.

### Recommended defaults

| Client environment | Recommended mode | Reason |
|---|---|---|
| Blazor Server | PureOpaque | Simple session-based authentication without separate token rotation |
| Blazor WebAssembly with API | Hybrid | Access/refresh credentials with authoritative session control |
| .NET MAUI | Hybrid | Token lifecycle suitable for native clients |
| MVC / Razor Pages | PureOpaque | Natural integration with secure browser cookies and server-side sessions |
| API-oriented service client | PureJwt, when stateless authentication is appropriate | Independent JWT validation without mandatory session infrastructure |
| UAuthHub | Depends on its configured role and operation | Hosting the authentication system is not itself an authentication mode |

These recommendations describe suitable architectural choices.

They are not a guarantee that every listed mode is the current implementation default for that profile.

In particular, the `UAuthHub` profile should not be interpreted as an authentication mode. UAuthHub is the authentication host, and its supported operations may serve clients using different modes.

For the full comparison of validation and lifecycle guarantees, see [Authentication Modes](/docs/fundamentals/auth-modes).

## Profile resolution and request context

UltimateAuth can serve clients with different profiles through the same authentication infrastructure.

For example:

```text id="yktx8s"
Blazor Server Client
         |
         | Profile: BlazorServer
         |
         +--------------------+
                              |
Blazor WASM Client            |
         |                    |
         | Profile: BlazorWasm |
         |                    v
         +---------------> UAuthHub
                              |
MAUI Client                   |
         |                    |
         | Profile: Maui      |
         |                    |
         +--------------------+
                              |
                              v
                    AuthFlowContext
                              |
                              v
                    Effective Mode
                    Effective Policy
                    Response Behavior
```

The server resolves the effective authentication context for each applicable operation.

This can include the Client Profile, authentication mode, tenant, security options, and credential delivery requirements.

Different clients can therefore operate under different authentication strategies without requiring separate authentication architectures.

### Per-request resolution does not mean arbitrary mode switching

The effective profile and mode may differ between requests from different clients.

However, they must remain consistent throughout an individual authentication operation.

A request must not begin under one authentication policy and complete under another because different components interpreted its profile differently.

Likewise, a client must not be allowed to change its declared profile to bypass validation requirements associated with an existing credential or session.

Authentication continuation must preserve the relevant security relationships established by the original authentication context.

The framework-controlled `AuthFlowContext` provides a consistent boundary for these decisions.

See [Flow-Based Authentication](/docs/fundamentals/flow-based-auth) for the conceptual model and [Auth Flows](/docs/auth-flows/overview) for the technical request pipeline.

## Credential delivery and runtime capabilities

Different clients have different capabilities for receiving and storing authentication credentials.

Client Profiles help the framework account for those differences.

### Browser environments

Browser applications can use cookies for authentication.

When cookies carry sensitive authentication credentials, appropriate `HttpOnly`, `Secure`, and `SameSite` policies should be applied.

Cookie-based authentication also requires appropriate CSRF protection.

In browser environments, a credential stored in an HttpOnly cookie is not directly readable by JavaScript or WebAssembly application code, although the browser can attach it to applicable requests.

### Native applications

Native applications generally communicate through HTTP clients and can use platform-specific secure credential storage.

Their credential transport and storage requirements differ from those of a browser.

A native Client Profile can help select appropriate integration behavior, but secure storage remains an application and platform responsibility.

### Server-hosted applications

Server-hosted applications can manage authentication state without exposing all authentication implementation details to browser application code.

This often makes session-oriented authentication attractive.

However, server execution alone does not guarantee security. Cookie protection, session validation, authorization, and long-lived connection behavior must still be handled correctly.

### Resource APIs

Resource APIs validate the credentials presented by callers.

Their authentication requirements must be enforced by trusted server configuration.

A caller's declared Client Profile must not determine whether token signatures, session validity, tenant boundaries, or authorization requirements can be bypassed.

## Security and trust boundaries

Client Profiles must preserve a clear separation between runtime adaptation and security authority.

### Profile declarations are not trusted identity

A Client Profile does not prove that the caller is a particular application, device, or user.

Additional authentication mechanisms are required when an application needs to establish client identity.

### Mode selection is server-controlled

A client may provide information relevant to mode resolution, but the server determines which authentication modes are permitted.

A profile must not provide a path to downgrade authentication requirements.

### Credential validity is independent of profile labels

A valid-looking credential must still satisfy the validation requirements of its authentication mode.

Changing a profile label must not turn an invalid credential into a valid one.

### Transport configuration is not authorization

Selecting cookies, headers, or another supported delivery mechanism does not grant access to protected resources.

Authentication and authorization decisions remain separate from credential transport.

### Defaults are not universal guarantees

A secure default depends on the application's deployment, runtime, authentication mode, and security requirements.

UltimateAuth can provide suitable defaults, but developers remain responsible for selecting and configuring the policies appropriate to their applications.

## Custom environments and extensibility

Not every application fits a predefined runtime category.

A custom host, embedded client, hybrid application, or specialized integration may need explicit profile configuration or additional integration behavior.

The purpose of Client Profiles is to keep these differences at a defined architectural boundary.

New integrations should reuse the existing authentication model and flow infrastructure wherever possible, rather than introducing independent authentication implementations.

Custom profile resolution and integration behavior must continue to respect server-controlled mode selection and authentication security policies.

## Summary

Client Profiles allow UltimateAuth to adapt authentication integration to different application environments.

They provide a consistent way to describe runtime characteristics and influence suitable defaults without turning client-provided metadata into a security authority.

The fundamental distinctions are:

**Client Profile** describes the runtime environment and integration requirements.

**Authentication Mode** defines credential validation, session dependencies, and authentication lifecycle guarantees.

**AuthFlowContext** establishes the effective security context for an authentication operation.

**Server policy** determines which behavior is permitted.

Together, these concepts allow different clients to share the same UltimateAuth infrastructure while preserving explicit authentication boundaries.

## Next step

Continue to [Runtime Architecture](/docs/fundamentals/runtime-architecture) to understand how UltimateAuth's client, server, and hosting components work together across different application environments.